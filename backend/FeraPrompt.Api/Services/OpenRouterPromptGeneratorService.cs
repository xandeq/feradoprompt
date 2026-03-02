
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FeraPrompt.Api.Data;
using FeraPrompt.Api.Models;
using FeraPrompt.Api.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FeraPrompt.Api.Services;

public class OpenRouterPromptGeneratorService : IPromptGeneratorService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterPromptGeneratorService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly IReadOnlyList<FreeModelCatalogItemViewModel> CuratedFreeModels =
        new List<FreeModelCatalogItemViewModel>
        {
            new() { Category = "general-reasoning", ModelId = "openrouter/free", Provider = "OpenRouter", MaxContext = "up to ~200k", Capabilities = "Auto-routing to free models", Limitations = "No deterministic model control." },
            new() { Category = "general-reasoning", ModelId = "meta-llama/meta-llama-3.3-70b-instruct:free", Provider = "Meta", MaxContext = "not specified", Capabilities = "Strong multilingual instruction-following", Limitations = "Text-only and rate-limited." },
            new() { Category = "general-reasoning", ModelId = "openai/gpt-oss-120b:free", Provider = "OpenAI", MaxContext = "not specified", Capabilities = "Strong reasoning and structured outputs", Limitations = "Free-tier limits can be strict." },
            new() { Category = "general-reasoning", ModelId = "openai/gpt-oss-20b:free", Provider = "OpenAI", MaxContext = "not specified", Capabilities = "Fast reasoning and tool use", Limitations = "Lower quality than larger variants." },
            new() { Category = "multimodal-vision", ModelId = "qwen/qwen3-vl-235b-a22b-thinking:free", Provider = "Qwen", MaxContext = "not specified", Capabilities = "Image/video reasoning and OCR", Limitations = "Heavy and slower." },
            new() { Category = "multimodal-vision", ModelId = "qwen/qwen3-vl-30b-a3b-thinking:free", Provider = "Qwen", MaxContext = "not specified", Capabilities = "Multimodal reasoning", Limitations = "Can be slow under load." },
            new() { Category = "multimodal-vision", ModelId = "mistralai/mistral-small-3.2-24b:free", Provider = "Mistral", MaxContext = "96k", Capabilities = "Balanced text+vision", Limitations = "Smaller context than some alternatives." },
            new() { Category = "heavy-reasoning-thinking", ModelId = "deepseek/deepseek-r1:free", Provider = "DeepSeek", MaxContext = "163k", Capabilities = "Deep reasoning", Limitations = "Can be verbose and slow." },
            new() { Category = "heavy-reasoning-thinking", ModelId = "qwen/qwen3-235b-a22b-thinking-2507:free", Provider = "Qwen", MaxContext = "262k", Capabilities = "Long-context reasoning", Limitations = "Thinking-oriented latency." },
            new() { Category = "coding", ModelId = "mistralai/devstral-small:free", Provider = "Mistral", MaxContext = "32k", Capabilities = "Code generation and agents", Limitations = "Smaller context for large repos." },
            new() { Category = "coding", ModelId = "qwen/qwen3-coder-480b:free", Provider = "Qwen", MaxContext = "262k", Capabilities = "Strong coding quality", Limitations = "Very heavy model." },
            new() { Category = "text-only-popular", ModelId = "tencent/hunyuan-a13b-instruct:free", Provider = "Tencent", MaxContext = "32k", Capabilities = "General instruction-following", Limitations = "PT-BR quality may vary." },
            new() { Category = "text-only-popular", ModelId = "venice/venice-uncensored:free", Provider = "Venice", MaxContext = "32k", Capabilities = "Loose safety profile", Limitations = "Quality variance by task." }
        };

    private static readonly string[] FallbackFreeModels = CuratedFreeModels.Select(x => x.ModelId).ToArray();

    public OpenRouterPromptGeneratorService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenRouterPromptGeneratorService> logger,
        IServiceProvider serviceProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public IReadOnlyList<FreeModelCatalogItemViewModel> GetCuratedFreeModels() => CuratedFreeModels;

    public async Task<IReadOnlyList<string>> GetFreeModelsAsync(string? apiKey = null)
    {
        var effectiveApiKey = ResolveApiKey(apiKey);
        if (string.IsNullOrWhiteSpace(effectiveApiKey))
        {
            return FallbackFreeModels;
        }

        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            var modelsUrl = $"{GetBaseUrl().TrimEnd('/')}/models";
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, modelsUrl);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", effectiveApiKey);
            requestMessage.Headers.TryAddWithoutValidation("HTTP-Referer", _configuration["OpenRouter:Referer"] ?? "https://feradoprompt.local");
            requestMessage.Headers.TryAddWithoutValidation("X-Title", _configuration["OpenRouter:AppTitle"] ?? "Fera do Prompt");

            var response = await httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch OpenRouter models. Status: {Status}", response.StatusCode);
                return FallbackFreeModels;
            }

            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                return FallbackFreeModels;
            }

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return FallbackFreeModels;
            }

            var freeModels = new List<string>();
            foreach (var item in data.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out var idProp))
                {
                    continue;
                }

                var modelId = idProp.GetString();
                if (string.IsNullOrWhiteSpace(modelId))
                {
                    continue;
                }

                if (modelId.Contains(":free", StringComparison.OrdinalIgnoreCase) || IsPricingFree(item))
                {
                    freeModels.Add(modelId);
                }
            }

            var ordered = freeModels.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(m => m, StringComparer.OrdinalIgnoreCase).Take(60).ToList();
            return ordered.Count > 0 ? ordered : FallbackFreeModels;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching free models from OpenRouter");
            return FallbackFreeModels;
        }
    }

    public async Task<PromptGenerationResponseViewModel> GenerateAsync(PromptGenerationRequestViewModel request)
    {
        var effectiveApiKey = ResolveApiKey(request.ApiKey);
        if (string.IsNullOrWhiteSpace(effectiveApiKey))
        {
            throw new InvalidOperationException("OpenRouter API key is not configured");
        }

        var clientIp = GetClientIpAddress();
        await EnforceDailyQuotaAsync(clientIp);

        var totalStopwatch = Stopwatch.StartNew();
        var systemPrompt = BuildSystemPrompt(request.Purpose);
        var userPrompt = BuildUserPrompt(request);
        var availableModels = await GetFreeModelsAsync(effectiveApiKey);
        var candidateModels = await BuildCandidateModelsAsync(request.Purpose, request.Model, availableModels);
        var maxAttempts = Math.Clamp(_configuration.GetValue<int?>("PromptGenerator:MaxFallbackAttempts") ?? 3, 1, 6);

        var attemptedModels = new List<string>();
        ModelAttemptResult? lastFailure = null;

        foreach (var modelId in candidateModels.Take(maxAttempts))
        {
            var attemptStopwatch = Stopwatch.StartNew();
            var attempt = await TryGenerateWithModelAsync(modelId, systemPrompt, userPrompt, effectiveApiKey);
            attemptStopwatch.Stop();

            attemptedModels.Add(modelId);
            await UpdateModelStatsAsync(request.Purpose, modelId, attempt.Success, (int)attemptStopwatch.ElapsedMilliseconds, attempt.StatusCode, attempt.ErrorMessage);

            if (attempt.Success && !string.IsNullOrWhiteSpace(attempt.GeneratedPrompt))
            {
                totalStopwatch.Stop();
                var generationId = await SaveGenerationRecordAsync(request, clientIp, attemptedModels, attempt, (int)totalStopwatch.ElapsedMilliseconds);
                return new PromptGenerationResponseViewModel
                {
                    Purpose = request.Purpose,
                    Model = modelId,
                    GeneratedPrompt = attempt.GeneratedPrompt.Trim(),
                    GenerationId = generationId,
                    UsedFallback = attemptedModels.Count > 1,
                    Attempts = attemptedModels.Count,
                    DurationMs = (int)totalStopwatch.ElapsedMilliseconds,
                    AttemptedModels = attemptedModels
                };
            }

            lastFailure = attempt;
            if (!ShouldFallback(attempt.StatusCode))
            {
                break;
            }
        }

        totalStopwatch.Stop();
        var fallbackFailure = lastFailure ?? new ModelAttemptResult(false, request.Model, string.Empty, 500, "Unknown OpenRouter failure");
        await SaveGenerationRecordAsync(request, clientIp, attemptedModels, fallbackFailure, (int)totalStopwatch.ElapsedMilliseconds);

        throw new HttpRequestException(
            $"OpenRouter error: {(HttpStatusCode)fallbackFailure.StatusCode} - {fallbackFailure.ErrorMessage}",
            null,
            (HttpStatusCode?)fallbackFailure.StatusCode);
    }

    public async Task<IReadOnlyList<ModelRankingItemViewModel>> GetModelRankingsAsync(string? purpose = null)
    {
        var db = TryGetDbContext();
        if (db == null)
        {
            return Array.Empty<ModelRankingItemViewModel>();
        }
        try
        {
            var query = db.ModelPerformanceStats.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(purpose))
            {
                var normalizedPurpose = purpose.Trim().ToLowerInvariant();
                query = query.Where(x => x.Purpose == normalizedPurpose);
            }

            var stats = await query.ToListAsync();
            return stats
                .Select(s => new ModelRankingItemViewModel
                {
                    ModelId = s.ModelId,
                    Score = ComputeScore(s),
                    TotalRequests = s.TotalRequests,
                    SuccessRate = s.TotalRequests == 0 ? 0d : (double)s.SuccessCount / s.TotalRequests,
                    AvgLatencyMs = s.TotalRequests == 0 ? 0d : (double)s.TotalLatencyMs / s.TotalRequests,
                    ConsecutiveFailures = s.ConsecutiveFailures
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.TotalRequests)
                .Take(20)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Model ranking unavailable");
            return Array.Empty<ModelRankingItemViewModel>();
        }
    }

    public async Task<PromptGeneratorHealthViewModel> GetHealthAsync()
    {
        var db = TryGetDbContext();
        if (db == null)
        {
            return new PromptGeneratorHealthViewModel
            {
                DailyQuotaPerIp = Math.Max(1, _configuration.GetValue<int?>("PromptGenerator:DailyQuotaPerIp") ?? 300)
            };
        }
        try
        {
            var since = DateTime.UtcNow.AddHours(-24);
            var records = await db.PromptGenerationRecords.AsNoTracking().Where(x => x.CreatedAt >= since).ToListAsync();

            var count = records.Count;
            var successCount = records.Count(x => x.Success);
            var fallbackCount = records.Count(x => x.UsedFallback);
            var avgLatency = count == 0 ? 0d : records.Average(x => x.DurationMs);

            return new PromptGeneratorHealthViewModel
            {
                RequestsLast24h = count,
                SuccessRateLast24h = count == 0 ? 0d : (double)successCount / count,
                AvgLatencyMsLast24h = Math.Round(avgLatency, 2),
                FallbackRateLast24h = count == 0 ? 0d : (double)fallbackCount / count,
                DailyQuotaPerIp = Math.Max(1, _configuration.GetValue<int?>("PromptGenerator:DailyQuotaPerIp") ?? 300),
                TopRankedModels = await GetModelRankingsAsync()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Prompt generator health data unavailable");
            return new PromptGeneratorHealthViewModel
            {
                DailyQuotaPerIp = Math.Max(1, _configuration.GetValue<int?>("PromptGenerator:DailyQuotaPerIp") ?? 300)
            };
        }
    }

    public async Task<IReadOnlyList<PromptGenerationHistoryItemViewModel>> GetHistoryAsync(string? sessionId, int limit)
    {
        var db = TryGetDbContext();
        if (db == null)
        {
            return Array.Empty<PromptGenerationHistoryItemViewModel>();
        }
        try
        {
            var safeLimit = Math.Clamp(limit, 1, 100);
            var query = db.PromptGenerationRecords.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                query = query.Where(x => x.ClientSessionId == sessionId);
            }

            var records = await query.OrderByDescending(x => x.CreatedAt).Take(safeLimit).ToListAsync();
            var parentIds = records.Where(x => x.ParentRecordId.HasValue).Select(x => x.ParentRecordId!.Value).Distinct().ToList();
            var parentMap = await db.PromptGenerationRecords.AsNoTracking().Where(x => parentIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.PublicId.ToString());
            return records.Select(r => MapHistoryItem(r, parentMap.TryGetValue(r.ParentRecordId ?? -1, out var parent) ? parent : null)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Prompt history unavailable");
            return Array.Empty<PromptGenerationHistoryItemViewModel>();
        }
    }

    public async Task<PromptGenerationHistoryItemViewModel?> GetHistoryByIdAsync(string id)
    {
        var db = TryGetDbContext();
        if (db == null || !Guid.TryParse(id, out var publicId))
        {
            return null;
        }
        try
        {
            var record = await db.PromptGenerationRecords.AsNoTracking().FirstOrDefaultAsync(x => x.PublicId == publicId);
            if (record == null)
            {
                return null;
            }

            string? parent = null;
            if (record.ParentRecordId.HasValue)
            {
                parent = await db.PromptGenerationRecords.AsNoTracking().Where(x => x.Id == record.ParentRecordId.Value).Select(x => x.PublicId.ToString()).FirstOrDefaultAsync();
            }

            return MapHistoryItem(record, parent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Prompt history item unavailable: {PublicId}", id);
            return null;
        }
    }

    public async Task<PromptGenerationHistoryItemViewModel?> DuplicateHistoryAsync(string id)
    {
        var db = TryGetDbContext();
        if (db == null || !Guid.TryParse(id, out var sourcePublicId))
        {
            return null;
        }
        try
        {
            var source = await db.PromptGenerationRecords.FirstOrDefaultAsync(x => x.PublicId == sourcePublicId);
            if (source == null)
            {
                return null;
            }

            var duplicate = new PromptGenerationRecord
            {
                ParentRecordId = source.Id,
                Version = source.Version + 1,
                Purpose = source.Purpose,
                RequestedModel = source.RequestedModel,
                FinalModel = source.FinalModel,
                Brief = source.Brief,
                ExtraContext = source.ExtraContext,
                Language = source.Language,
                Style = source.Style,
                Duration = source.Duration,
                AspectRatio = source.AspectRatio,
                Success = source.Success,
                GeneratedPrompt = source.GeneratedPrompt,
                ErrorMessage = source.ErrorMessage,
                UsedFallback = source.UsedFallback,
                Attempts = source.Attempts,
                DurationMs = source.DurationMs,
                AttemptedModelsJson = source.AttemptedModelsJson,
                ClientSessionId = source.ClientSessionId,
                RequestIp = source.RequestIp,
                CreatedAt = DateTime.UtcNow
            };

            db.PromptGenerationRecords.Add(duplicate);
            await db.SaveChangesAsync();
            return MapHistoryItem(duplicate, source.PublicId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to duplicate prompt history item: {PublicId}", id);
            return null;
        }
    }

    public async Task<PromptGenerationComparisonViewModel?> CompareHistoryAsync(string leftId, string rightId)
    {
        var left = await GetHistoryByIdAsync(leftId);
        var right = await GetHistoryByIdAsync(rightId);
        if (left == null || right == null)
        {
            return null;
        }

        var leftLines = SplitLines(left.GeneratedPrompt);
        var rightLines = SplitLines(right.GeneratedPrompt);
        var changedEstimate = leftLines.Except(rightLines, StringComparer.Ordinal).Count() + rightLines.Except(leftLines, StringComparer.Ordinal).Count();

        return new PromptGenerationComparisonViewModel
        {
            Left = left,
            Right = right,
            SameFinalModel = string.Equals(left.FinalModel, right.FinalModel, StringComparison.OrdinalIgnoreCase),
            OutputLengthDelta = right.GeneratedPrompt.Length - left.GeneratedPrompt.Length,
            ChangedLineCountEstimate = changedEstimate
        };
    }

    private async Task<IReadOnlyList<string>> BuildCandidateModelsAsync(string purpose, string requestedModel, IReadOnlyList<string> freeModels)
    {
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(requestedModel))
        {
            list.Add(requestedModel.Trim());
        }

        var ranked = await GetModelRankingsAsync(purpose);
        foreach (var item in ranked)
        {
            list.Add(item.ModelId);
        }

        foreach (var model in PurposeDefaults(purpose))
        {
            list.Add(model);
        }

        foreach (var model in freeModels)
        {
            list.Add(model);
        }

        return list.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList();
    }

    private static IReadOnlyList<string> PurposeDefaults(string purpose)
    {
        var normalized = purpose.Trim().ToLowerInvariant();
        return normalized switch
        {
            "nano_banana_image" => new[] { "openrouter/free", "qwen/qwen3-vl-30b-a3b-thinking:free", "mistralai/mistral-small-3.2-24b:free" },
            "chatgpt_images" => new[] { "openrouter/free", "meta-llama/meta-llama-3.3-70b-instruct:free", "openai/gpt-oss-20b:free" },
            "veo3_video_script" => new[] { "openrouter/free", "deepseek/deepseek-r1:free", "openai/gpt-oss-120b:free" },
            "veo3_video_json" => new[] { "openrouter/free", "mistralai/devstral-small:free", "openai/gpt-oss-20b:free" },
            "grok_imagine_image" => new[] { "openrouter/free", "qwen/qwen3-vl-30b-a3b-thinking:free", "meta-llama/meta-llama-3.3-70b-instruct:free" },
            "grok_imagine_animate" => new[] { "openrouter/free", "deepseek/deepseek-r1:free", "qwen/qwen3-vl-30b-a3b-thinking:free" },
            _ => new[] { "openrouter/free" }
        };
    }

    private async Task<ModelAttemptResult> TryGenerateWithModelAsync(string modelId, string systemPrompt, string userPrompt, string apiKey)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(90);
            var payload = new
            {
                model = modelId,
                messages = new object[] { new { role = "system", content = systemPrompt }, new { role = "user", content = userPrompt } },
                temperature = 0.4,
                max_tokens = 1800
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var completionsUrl = $"{GetBaseUrl().TrimEnd('/')}/chat/completions";
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, completionsUrl)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            requestMessage.Headers.TryAddWithoutValidation("HTTP-Referer", _configuration["OpenRouter:Referer"] ?? "https://feradoprompt.local");
            requestMessage.Headers.TryAddWithoutValidation("X-Title", _configuration["OpenRouter:AppTitle"] ?? "Fera do Prompt");

            var response = await httpClient.SendAsync(requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();
            var statusCode = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                return new ModelAttemptResult(false, modelId, string.Empty, statusCode, responseBody);
            }

            var generated = ExtractMessageContent(responseBody);
            if (string.IsNullOrWhiteSpace(generated))
            {
                return new ModelAttemptResult(false, modelId, string.Empty, 502, "OpenRouter returned empty content");
            }

            return new ModelAttemptResult(true, modelId, generated.Trim(), statusCode, string.Empty);
        }
        catch (TaskCanceledException)
        {
            return new ModelAttemptResult(false, modelId, string.Empty, 504, "Timeout calling OpenRouter");
        }
        catch (Exception ex)
        {
            return new ModelAttemptResult(false, modelId, string.Empty, 500, ex.Message);
        }
    }

    private async Task UpdateModelStatsAsync(string purpose, string modelId, bool success, int latencyMs, int statusCode, string? error)
    {
        var db = TryGetDbContext();
        if (db == null)
        {
            return;
        }

        try
        {
            var normalizedPurpose = purpose.Trim().ToLowerInvariant();
            var row = await db.ModelPerformanceStats.FirstOrDefaultAsync(x => x.Purpose == normalizedPurpose && x.ModelId == modelId);
            if (row == null)
            {
                row = new ModelPerformanceStat { Purpose = normalizedPurpose, ModelId = modelId };
                db.ModelPerformanceStats.Add(row);
            }

            row.TotalRequests += 1;
            row.TotalLatencyMs += latencyMs;
            row.LastStatusCode = statusCode;
            row.LastUsedAt = DateTime.UtcNow;
            if (success)
            {
                row.SuccessCount += 1;
                row.ConsecutiveFailures = 0;
                row.LastError = null;
            }
            else
            {
                row.FailureCount += 1;
                row.ConsecutiveFailures += 1;
                row.LastError = Truncate(error, 500);
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update model stats for {ModelId}", modelId);
        }
    }

    private async Task<string?> SaveGenerationRecordAsync(PromptGenerationRequestViewModel request, string clientIp, IReadOnlyList<string> attemptedModels, ModelAttemptResult attempt, int durationMs)
    {
        var db = TryGetDbContext();
        if (db == null)
        {
            return null;
        }

        try
        {
            PromptGenerationRecord? parent = null;
            if (!string.IsNullOrWhiteSpace(request.ParentGenerationId) && Guid.TryParse(request.ParentGenerationId, out var parentPublicId))
            {
                parent = await db.PromptGenerationRecords.FirstOrDefaultAsync(x => x.PublicId == parentPublicId);
            }

            var record = new PromptGenerationRecord
            {
                ParentRecordId = parent?.Id,
                Version = parent == null ? 1 : parent.Version + 1,
                Purpose = request.Purpose,
                RequestedModel = request.Model,
                FinalModel = attempt.ModelId,
                Brief = request.Brief,
                ExtraContext = request.ExtraContext,
                Language = request.Language,
                Style = request.Style,
                Duration = request.Duration,
                AspectRatio = request.AspectRatio,
                Success = attempt.Success,
                GeneratedPrompt = attempt.GeneratedPrompt,
                ErrorMessage = attempt.Success ? null : Truncate(attempt.ErrorMessage, 2000),
                UsedFallback = attemptedModels.Count > 1,
                Attempts = attemptedModels.Count,
                DurationMs = durationMs,
                AttemptedModelsJson = JsonSerializer.Serialize(attemptedModels),
                ClientSessionId = request.ClientSessionId,
                RequestIp = clientIp,
                CreatedAt = DateTime.UtcNow
            };

            db.PromptGenerationRecords.Add(record);
            await db.SaveChangesAsync();
            return record.PublicId.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save prompt generation history");
            return null;
        }
    }

    private async Task EnforceDailyQuotaAsync(string clientIp)
    {
        var db = TryGetDbContext();
        if (db == null)
        {
            return;
        }

        try
        {
            var limit = Math.Max(1, _configuration.GetValue<int?>("PromptGenerator:DailyQuotaPerIp") ?? 300);
            var today = DateTime.UtcNow.Date;
            var usage = await db.DailyQuotaUsages.FirstOrDefaultAsync(x => x.DateUtc == today && x.IpAddress == clientIp);
            if (usage == null)
            {
                usage = new DailyQuotaUsage { DateUtc = today, IpAddress = clientIp, Count = 1, UpdatedAt = DateTime.UtcNow };
                db.DailyQuotaUsages.Add(usage);
                await db.SaveChangesAsync();
                return;
            }

            if (usage.Count >= limit)
            {
                throw new HttpRequestException($"Daily quota exceeded for IP {clientIp}. Limit={limit}", null, HttpStatusCode.TooManyRequests);
            }

            usage.Count += 1;
            usage.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Quota enforcement unavailable, continuing request");
        }
    }

    private ApplicationDbContext? TryGetDbContext() => _serviceProvider.GetService<ApplicationDbContext>();

    private string GetClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return "unknown";
        }

        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool ShouldFallback(int statusCode) => statusCode == 408 || statusCode == 409 || statusCode == 425 || statusCode == 429 || statusCode >= 500;

    private static double ComputeScore(ModelPerformanceStat stat)
    {
        if (stat.TotalRequests <= 0)
        {
            return 0.5d;
        }

        var successRate = (double)stat.SuccessCount / stat.TotalRequests;
        var avgLatencyMs = (double)stat.TotalLatencyMs / stat.TotalRequests;
        var latencyScore = 1d / (1d + (avgLatencyMs / 5000d));
        var failurePenalty = Math.Min(stat.ConsecutiveFailures * 0.08d, 0.4d);
        var raw = (successRate * 0.65d) + (latencyScore * 0.35d) - failurePenalty;
        return Math.Max(0d, Math.Round(raw, 4));
    }

    private static PromptGenerationHistoryItemViewModel MapHistoryItem(PromptGenerationRecord record, string? parentPublicId) =>
        new()
        {
            Id = record.PublicId.ToString(),
            ParentId = parentPublicId,
            Version = record.Version,
            Purpose = record.Purpose,
            RequestedModel = record.RequestedModel,
            FinalModel = record.FinalModel,
            Brief = record.Brief,
            GeneratedPrompt = record.GeneratedPrompt,
            Success = record.Success,
            UsedFallback = record.UsedFallback,
            Attempts = record.Attempts,
            DurationMs = record.DurationMs,
            ErrorMessage = record.ErrorMessage,
            CreatedAt = record.CreatedAt
        };

    private static IEnumerable<string> SplitLines(string value) =>
        value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x));

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string NormalizeApiKey(string? rawApiKey)
    {
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            return string.Empty;
        }

        var value = rawApiKey.Trim().Trim('"').Trim('\'');
        if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            value = value.Substring("Bearer ".Length).Trim();
        }

        value = Regex.Replace(value, @"\s+", string.Empty);
        if (!value.StartsWith("sk-or-v1-", StringComparison.OrdinalIgnoreCase) && Regex.IsMatch(value, "^[a-fA-F0-9]{64}$"))
        {
            value = $"sk-or-v1-{value}";
        }

        return value;
    }

    private string ResolveApiKey(string? requestApiKey)
    {
        var fromRequest = NormalizeApiKey(requestApiKey);
        if (!string.IsNullOrWhiteSpace(fromRequest) && IsLikelyOpenRouterApiKey(fromRequest))
        {
            return fromRequest;
        }

        var fromConfig = NormalizeApiKey(_configuration["OpenRouter:ApiKey"]);
        if (!string.IsNullOrWhiteSpace(fromConfig) && IsLikelyOpenRouterApiKey(fromConfig))
        {
            return fromConfig;
        }

        var fromEnvironment = NormalizeApiKey(Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"));
        if (!string.IsNullOrWhiteSpace(fromEnvironment) && IsLikelyOpenRouterApiKey(fromEnvironment))
        {
            return fromEnvironment;
        }

        return string.Empty;
    }

    private static bool IsLikelyOpenRouterApiKey(string value) => value.StartsWith("sk-or-v1-", StringComparison.OrdinalIgnoreCase);

    private string GetBaseUrl() => _configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1";

    private static bool IsPricingFree(JsonElement modelElement)
    {
        if (!modelElement.TryGetProperty("pricing", out var pricing) || pricing.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var promptZero = IsZeroStringOrNumber(pricing, "prompt");
        var completionZero = IsZeroStringOrNumber(pricing, "completion");
        return promptZero && completionZero;
    }

    private static bool IsZeroStringOrNumber(JsonElement parent, string fieldName)
    {
        if (!parent.TryGetProperty(fieldName, out var prop))
        {
            return false;
        }

        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.GetDouble() == 0d,
            JsonValueKind.String => double.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) && Math.Abs(v) < 0.0000001d,
            _ => false
        };
    }

    private static string ExtractMessageContent(string body)
    {
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var first = choices.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        if (first.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.Object && message.TryGetProperty("content", out var content))
        {
            return content.ToString();
        }

        return string.Empty;
    }

    private static string BuildSystemPrompt(string purpose)
    {
        var normalized = purpose.Trim().ToLowerInvariant();
        return normalized switch
        {
            "nano_banana_image" => "You are a prompt engineer specialized in image generation for Gemini/Nano Banana. Return only the final prompt.",
            "chatgpt_images" => "You are a prompt engineer specialized in ChatGPT Images. Return only the final prompt.",
            "veo3_video_script" => "You are a video script specialist for Google Veo 3. Return a clear production-ready script.",
            "veo3_video_json" => "You are a specialist in Veo 3 JSON output. Return valid JSON only.",
            "grok_imagine_image" => "You are a prompt engineer specialized in Grok Imagine static images.",
            "grok_imagine_animate" => "You are a prompt engineer specialized in Grok Imagine image animation.",
            _ => "You are an expert multimodal prompt engineer. Return objective copy-paste ready output."
        };
    }

    private static string BuildUserPrompt(PromptGenerationRequestViewModel request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Create the best possible output for the selected goal.");
        sb.AppendLine("Rules:");
        sb.AppendLine("1) Be specific and structured.");
        sb.AppendLine("2) Avoid explaining your process.");
        sb.AppendLine("3) Return only final output ready to copy.");
        sb.AppendLine();
        sb.AppendLine($"Purpose: {request.Purpose}");
        sb.AppendLine($"Brief: {request.Brief}");
        if (!string.IsNullOrWhiteSpace(request.ExtraContext)) sb.AppendLine($"ExtraContext: {request.ExtraContext}");
        if (!string.IsNullOrWhiteSpace(request.Style)) sb.AppendLine($"Style: {request.Style}");
        if (!string.IsNullOrWhiteSpace(request.Language)) sb.AppendLine($"Language: {request.Language}");
        if (!string.IsNullOrWhiteSpace(request.Duration)) sb.AppendLine($"Duration: {request.Duration}");
        if (!string.IsNullOrWhiteSpace(request.AspectRatio)) sb.AppendLine($"AspectRatio: {request.AspectRatio}");
        return sb.ToString().Trim();
    }

    private sealed record ModelAttemptResult(bool Success, string ModelId, string GeneratedPrompt, int StatusCode, string? ErrorMessage);
}
