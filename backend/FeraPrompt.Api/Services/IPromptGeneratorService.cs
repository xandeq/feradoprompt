using FeraPrompt.Api.ViewModels;

namespace FeraPrompt.Api.Services;

public interface IPromptGeneratorService
{
    IReadOnlyList<FreeModelCatalogItemViewModel> GetCuratedFreeModels();
    Task<IReadOnlyList<string>> GetFreeModelsAsync(string? apiKey = null);
    Task<PromptGenerationResponseViewModel> GenerateAsync(PromptGenerationRequestViewModel request);
}
