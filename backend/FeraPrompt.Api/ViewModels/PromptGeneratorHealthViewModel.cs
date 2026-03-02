namespace FeraPrompt.Api.ViewModels;

public class PromptGeneratorHealthViewModel
{
    public int RequestsLast24h { get; set; }
    public double SuccessRateLast24h { get; set; }
    public double AvgLatencyMsLast24h { get; set; }
    public double FallbackRateLast24h { get; set; }
    public int DailyQuotaPerIp { get; set; }
    public IReadOnlyList<ModelRankingItemViewModel> TopRankedModels { get; set; } = Array.Empty<ModelRankingItemViewModel>();
}
