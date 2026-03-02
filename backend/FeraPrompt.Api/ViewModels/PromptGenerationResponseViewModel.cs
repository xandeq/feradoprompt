namespace FeraPrompt.Api.ViewModels;

public class PromptGenerationResponseViewModel
{
    public string Provider { get; set; } = "openrouter";
    public string Purpose { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string GeneratedPrompt { get; set; } = string.Empty;
    public string? GenerationId { get; set; }
    public bool UsedFallback { get; set; }
    public int Attempts { get; set; }
    public int DurationMs { get; set; }
    public IReadOnlyList<string> AttemptedModels { get; set; } = Array.Empty<string>();
}
