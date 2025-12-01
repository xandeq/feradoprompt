namespace FeraPrompt.Api.Models;

/// <summary>
/// Representa uma requisição contendo o HTML a ser convertido em PDF.
/// </summary>
public class HtmlRequest
{
    /// <summary>
    /// Conteúdo HTML bruto que será renderizado pelo Chromium headless.
    /// </summary>
    public string Html { get; set; } = string.Empty;
}
