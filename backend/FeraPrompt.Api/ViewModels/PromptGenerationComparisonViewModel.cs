namespace FeraPrompt.Api.ViewModels;

public class PromptGenerationComparisonViewModel
{
    public PromptGenerationHistoryItemViewModel Left { get; set; } = new();
    public PromptGenerationHistoryItemViewModel Right { get; set; } = new();
    public bool SameFinalModel { get; set; }
    public int OutputLengthDelta { get; set; }
    public int ChangedLineCountEstimate { get; set; }
}
