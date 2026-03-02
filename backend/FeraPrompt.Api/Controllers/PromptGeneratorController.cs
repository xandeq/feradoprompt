using FeraPrompt.Api.Services;
using FeraPrompt.Api.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FeraPrompt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PromptGeneratorController : ControllerBase
{
    private readonly IPromptGeneratorService _promptGeneratorService;

    public PromptGeneratorController(IPromptGeneratorService promptGeneratorService)
    {
        _promptGeneratorService = promptGeneratorService;
    }

    [HttpGet("models/catalog")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<FreeModelCatalogItemViewModel>> GetModelCatalog()
    {
        return Ok(_promptGeneratorService.GetCuratedFreeModels());
    }

    [HttpPost("models/free")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetFreeModels([FromBody] FreeModelsRequestViewModel? request)
    {
        var models = await _promptGeneratorService.GetFreeModelsAsync(request?.ApiKey);
        return Ok(models);
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(PromptGenerationResponseViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PromptGenerationResponseViewModel>> Generate([FromBody] PromptGenerationRequestViewModel request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _promptGeneratorService.GenerateAsync(request);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
            }
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("models/ranking")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ModelRankingItemViewModel>>> GetModelRanking([FromQuery] string? purpose = null)
    {
        var ranking = await _promptGeneratorService.GetModelRankingsAsync(purpose);
        return Ok(ranking);
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PromptGeneratorHealthViewModel>> GetHealth()
    {
        var health = await _promptGeneratorService.GetHealthAsync();
        return Ok(health);
    }

    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PromptGenerationHistoryItemViewModel>>> GetHistory([FromQuery] string? sessionId = null, [FromQuery] int limit = 30)
    {
        var history = await _promptGeneratorService.GetHistoryAsync(sessionId, limit);
        return Ok(history);
    }

    [HttpGet("history/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromptGenerationHistoryItemViewModel>> GetHistoryById(string id)
    {
        var history = await _promptGeneratorService.GetHistoryByIdAsync(id);
        if (history == null)
        {
            return NotFound(new { message = "History item not found" });
        }

        return Ok(history);
    }

    [HttpPost("history/{id}/duplicate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromptGenerationHistoryItemViewModel>> DuplicateHistory(string id)
    {
        var duplicate = await _promptGeneratorService.DuplicateHistoryAsync(id);
        if (duplicate == null)
        {
            return NotFound(new { message = "History item not found for duplication" });
        }

        return Ok(duplicate);
    }

    [HttpPost("history/{id}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromptGenerationHistoryItemViewModel>> RestoreHistory(string id)
    {
        var history = await _promptGeneratorService.GetHistoryByIdAsync(id);
        if (history == null)
        {
            return NotFound(new { message = "History item not found for restore" });
        }

        return Ok(history);
    }

    [HttpGet("history/compare")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromptGenerationComparisonViewModel>> CompareHistory([FromQuery] string leftId, [FromQuery] string rightId)
    {
        var comparison = await _promptGeneratorService.CompareHistoryAsync(leftId, rightId);
        if (comparison == null)
        {
            return NotFound(new { message = "Comparison items not found" });
        }

        return Ok(comparison);
    }
}
