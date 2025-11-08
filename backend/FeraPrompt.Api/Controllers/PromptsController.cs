using Microsoft.AspNetCore.Mvc;
using FeraPrompt.Api.Models;
using FeraPrompt.Api.Services;
using FeraPrompt.Api.ViewModels;

namespace FeraPrompt.Api.Controllers;

/// <summary>
/// Controller para gerenciamento de Prompts com integração n8n
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PromptsController : ControllerBase
{
    private readonly IPromptService _promptService;
    private readonly ILogger<PromptsController> _logger;

    public PromptsController(IPromptService promptService, ILogger<PromptsController> logger)
    {
        _promptService = promptService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os prompts
    /// </summary>
    /// <returns>Lista de prompts</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Prompt>>> GetAll()
    {
        var prompts = await _promptService.GetAllPromptsAsync();
        return Ok(prompts);
    }

    /// <summary>
    /// Busca um prompt por ID com histórico
    /// </summary>
    /// <param name="id">ID do prompt</param>
    /// <returns>Prompt encontrado com histórico de execuções</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Prompt>> GetById(int id)
    {
        var prompt = await _promptService.GetByIdAsync(id);

        if (prompt == null)
        {
            _logger.LogWarning("Prompt não encontrado: {PromptId}", id);
            return NotFound(new { message = $"Prompt com ID {id} não encontrado" });
        }

        return Ok(prompt);
    }

    /// <summary>
    /// Cria um novo prompt
    /// </summary>
    /// <param name="model">Dados do prompt</param>
    /// <returns>Prompt criado</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Prompt>> Create([FromBody] PromptCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var created = await _promptService.CreatePromptAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar prompt");
            return BadRequest(new { message = "Erro ao criar prompt", error = ex.Message });
        }
    }

    /// <summary>
    /// Executa um prompt enviando para o n8n
    /// </summary>
    /// <param name="model">Dados para execução do prompt</param>
    /// <returns>Resposta com output processado</returns>
    [HttpPost("execute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PromptResponseViewModel>> Execute([FromBody] PromptRunViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _promptService.ExecutePromptAsync(model);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Prompt não encontrado: {PromptId}", model.PromptId);
            return NotFound(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao chamar webhook n8n para prompt {PromptId}", model.PromptId);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Erro ao comunicar com o serviço n8n", error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar prompt {PromptId}", model.PromptId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Erro ao executar prompt", error = ex.Message });
        }
    }

    /// <summary>
    /// Deleta um prompt
    /// </summary>
    /// <param name="id">ID do prompt</param>
    /// <returns>Status da operação</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _promptService.DeletePromptAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Tentativa de deletar prompt inexistente: {PromptId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar prompt {PromptId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Erro ao deletar prompt", error = ex.Message });
        }
    }
}
