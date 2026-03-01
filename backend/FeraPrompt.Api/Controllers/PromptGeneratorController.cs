using FeraPrompt.Api.Services;
using FeraPrompt.Api.ViewModels;
using Microsoft.AspNetCore.Mvc;

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
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
