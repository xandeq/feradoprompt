using FeraPrompt.Api.ViewModels;

namespace FeraPrompt.Api.Services;

public interface IPromptGeneratorService
{
    IReadOnlyList<FreeModelCatalogItemViewModel> GetCuratedFreeModels();
    Task<IReadOnlyList<string>> GetFreeModelsAsync(string? apiKey = null);
    Task<PromptGenerationResponseViewModel> GenerateAsync(PromptGenerationRequestViewModel request);
    Task<IReadOnlyList<ModelRankingItemViewModel>> GetModelRankingsAsync(string? purpose = null);
    Task<PromptGeneratorHealthViewModel> GetHealthAsync();
    Task<IReadOnlyList<PromptGenerationHistoryItemViewModel>> GetHistoryAsync(string? sessionId, int limit);
    Task<PromptGenerationHistoryItemViewModel?> GetHistoryByIdAsync(string id);
    Task<PromptGenerationHistoryItemViewModel?> DuplicateHistoryAsync(string id);
    Task<PromptGenerationComparisonViewModel?> CompareHistoryAsync(string leftId, string rightId);
}
