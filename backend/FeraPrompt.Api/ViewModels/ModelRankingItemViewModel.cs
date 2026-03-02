namespace FeraPrompt.Api.ViewModels;

public class ModelRankingItemViewModel
{
    public string ModelId { get; set; } = string.Empty;
    public double Score { get; set; }
    public int TotalRequests { get; set; }
    public double SuccessRate { get; set; }
    public double AvgLatencyMs { get; set; }
    public int ConsecutiveFailures { get; set; }
}
