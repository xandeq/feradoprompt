namespace FeraPrompt.Api.ViewModels;

public class PromptGenerationHistoryItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public int Version { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string RequestedModel { get; set; } = string.Empty;
    public string FinalModel { get; set; } = string.Empty;
    public string Brief { get; set; } = string.Empty;
    public string GeneratedPrompt { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool UsedFallback { get; set; }
    public int Attempts { get; set; }
    public int DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
