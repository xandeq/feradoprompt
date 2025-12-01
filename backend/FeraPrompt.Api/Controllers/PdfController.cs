using FeraPrompt.Api.Models;
using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using PuppeteerSharp.BrowserData;
using PuppeteerSharp.Media;

namespace FeraPrompt.Api.Controllers;

/// <summary>
/// Controller responsável por converter conteúdo HTML em PDF usando PuppeteerSharp.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PdfController : ControllerBase
{
    private readonly ILogger<PdfController> _logger;
    private readonly IWebHostEnvironment _environment;
    private static readonly SemaphoreSlim BrowserDownloadLock = new(1, 1);

    public PdfController(ILogger<PdfController> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Converte HTML em PDF utilizando Chromium headless.
    /// </summary>
    /// <param name="request">Objeto contendo o HTML bruto.</param>
    /// <returns>Arquivo PDF em formato A4.</returns>
    [HttpPost("convert")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/pdf")]
    public async Task<IActionResult> ConvertHtmlAsync([FromBody] HtmlRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Html))
        {
            ModelState.AddModelError(nameof(HtmlRequest.Html), "O HTML é obrigatório.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var executablePath = await EnsureChromiumAsync();

            var launchOptions = new LaunchOptions
            {
                Headless = true,
                ExecutablePath = executablePath,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox"
                }
            };

            await using var browser = await Puppeteer.LaunchAsync(launchOptions);
            await using var page = await browser.NewPageAsync();

            await page.SetContentAsync(request.Html, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            var pdfBytes = await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true
            });

            var fileName = $"document-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (PuppeteerException ex)
        {
            _logger.LogError(ex, "Falha ao renderizar PDF via PuppeteerSharp");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "Erro ao executar o Chromium headless",
                error = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao converter HTML em PDF");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Erro inesperado ao gerar PDF",
                error = ex.Message
            });
        }
    }

    private async Task<string> EnsureChromiumAsync()
    {
        // Garante que somente uma thread faça o download por vez
        await BrowserDownloadLock.WaitAsync();
        try
        {
            var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions
            {
                Browser = SupportedBrowser.Chrome,
                Path = Path.Combine(_environment.ContentRootPath, ".local-chromium")
            });

            var installedBrowser = await browserFetcher.DownloadAsync();
            var executablePath = installedBrowser.GetExecutablePath();

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new InvalidOperationException("Caminho do Chromium não foi encontrado após o download.");
            }

            return executablePath;
        }
        finally
        {
            BrowserDownloadLock.Release();
        }
    }
}
