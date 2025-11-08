namespace FeraPrompt.Api.ViewModels;

/// <summary>
/// ViewModel de resposta da execução de um Prompt
/// </summary>
public class PromptResponseViewModel
{
    public int PromptId { get; set; }
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
}
