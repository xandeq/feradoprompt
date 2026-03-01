using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using FeraPrompt.Api.ViewModels;

namespace FeraPrompt.Api.Services;

public class OpenRouterPromptGeneratorService : IPromptGeneratorService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenRouterPromptGeneratorService> _logger;

    private static readonly IReadOnlyList<FreeModelCatalogItemViewModel> CuratedFreeModels =
        new List<FreeModelCatalogItemViewModel>
        {
            new()
            {
                Category = "general-reasoning",
                ModelId = "openrouter/free",
                Provider = "OpenRouter",
                MaxContext = "ate ~200k (depends on routed model)",
                Capabilities = "Auto-routing to free models",
                Limitations = "No deterministic model control; performance varies by routed backend."
            },
            new()
            {
                Category = "general-reasoning",
                ModelId = "arcee-ai/trinity-large-preview:free",
                Provider = "Arcee AI",
                MaxContext = "128k (preview; architecture up to 512k)",
                Capabilities = "Chat, creative writing, roleplay, agentic workflows",
                Limitations = "Preview model, quantized variant, subject to free tier limits."
            },
            new()
            {
                Category = "general-reasoning",
                ModelId = "stepfun/step-3.5-flash:free",
                Provider = "StepFun",
                MaxContext = "not specified (long-context class)",
                Capabilities = "Fast reasoning chat for long inputs",
                Limitations = "Rate-limited in free tier; context specifics not fully exposed."
            },
            new()
            {
                Category = "general-reasoning",
                ModelId = "zhipu-ai/glm-4.5-air:free",
                Provider = "Z.ai",
                MaxContext = "not specified",
                Capabilities = "Reasoning, chat, optional thinking mode, tool usage",
                Limitations = "Thinking mode is parameter-controlled; free tier rate limits apply."
            },
            new()
            {
                Category = "general-reasoning",
                ModelId = "nvidia/nemotron-3-nano-30b-a3b:free",
                Provider = "NVIDIA",
                MaxContext = "not specified",
                Capabilities = "General reasoning chat, open-weight MoE, agentic use",
                Limitations = "Developer-oriented; daily/request caps on free usage."
            },
            new()
            {
                Category = "general-reasoning",
                ModelId = "nvidia/nemotron-nano-9b-v2:free",
                Provider = "NVIDIA",
                MaxContext = "not specified",
                Capabilities = "Light reasoning chat with controllable reasoning traces",
                Limitations = "Can produce verbose traces; free throughput limits."
            },
            new()
            {
                Category = "general-reasoning",
                ModelId = "openai/gpt-oss-120b:free",
                Provider = "OpenAI",
                MaxContext = "not specified",
                Capabilities = "Strong reasoning, tool use, function calling, structured outputs",
                Limitations = "Open-weight variant on free tier with rate limits."
            },
            new()
            {
                Category = "general-reasoning",
                ModelId = "openai/gpt-oss-20b:free",
                Provider = "OpenAI",
                MaxContext = "not specified",
                Capabilities = "Fast reasoning, tool use, structured outputs",
                Limitations = "Smaller than 120B; free usage constraints still apply."
            },
            new()
            {
                Category = "general-reasoning",
                ModelId = "meta-llama/meta-llama-3.3-70b-instruct:free",
                Provider = "Meta",
                MaxContext = "not specified (long context class)",
                Capabilities = "Multilingual instruction-following, robust dialogue",
                Limitations = "Text-only; free rate limits."
            },
            new()
            {
                Category = "general-reasoning",
                ModelId = "upstage/solar-pro-3:free",
                Provider = "Upstage",
                MaxContext = "not specified",
                Capabilities = "General chat (strong Korean/English/Japanese support)",
                Limitations = "PT-BR quality is not the primary focus; free limits."
            },
            new()
            {
                Category = "general-reasoning",
                ModelId = "arcee-ai/trinity-mini:free",
                Provider = "Arcee AI",
                MaxContext = "131k",
                Capabilities = "Chat, reasoning, function calling, long-context agentic workflows",
                Limitations = "Smaller than Trinity Large; free-tier limits."
            },
            new()
            {
                Category = "general-reasoning",
                ModelId = "lfms/lfm2.5-1.2b-thinking:free",
                Provider = "LFM",
                MaxContext = "32k",
                Capabilities = "Lightweight reasoning, RAG, extraction tasks",
                Limitations = "Lower quality ceiling vs larger models."
            },
            new()
            {
                Category = "multimodal-vision",
                ModelId = "qwen/qwen3-vl-235b-a22b-thinking:free",
                Provider = "Qwen",
                MaxContext = "not specified (long-context class)",
                Capabilities = "Vision-language reasoning across images and video, OCR, STEM",
                Limitations = "Heavy model; may be slower and rate-limited on free tier."
            },
            new()
            {
                Category = "multimodal-vision",
                ModelId = "qwen/qwen3-vl-30b-a3b-thinking:free",
                Provider = "Qwen",
                MaxContext = "not specified",
                Capabilities = "Multimodal reasoning with multi-image/video support",
                Limitations = "Smaller than 235B but still relatively heavy."
            },
            new()
            {
                Category = "multimodal-vision",
                ModelId = "nvidia/nemotron-nano-2-vl:free",
                Provider = "NVIDIA",
                MaxContext = "not specified",
                Capabilities = "Text + image/video understanding, OCR, document and diagram tasks",
                Limitations = "Best for OCR/diagram tasks; free throughput caps."
            },
            new()
            {
                Category = "multimodal-vision",
                ModelId = "mistralai/mistral-small-3.2-24b:free",
                Provider = "Mistral AI",
                MaxContext = "96k",
                Capabilities = "Text + vision, function calling, balanced quality",
                Limitations = "Smaller context than some paid variants."
            },
            new()
            {
                Category = "multimodal-vision",
                ModelId = "google/gemma-3:free",
                Provider = "Google",
                MaxContext = "up to 128k (variant-dependent)",
                Capabilities = "Vision-language, multilingual, reasoning, function calling",
                Limitations = "Exact limits vary by submodel variant."
            },
            new()
            {
                Category = "heavy-reasoning-thinking",
                ModelId = "qwen/qwen3-235b-a22b-thinking-2507:free",
                Provider = "Qwen",
                MaxContext = "262,144",
                Capabilities = "Heavy reasoning for math/science and long-form tasks",
                Limitations = "Thinking-oriented, may generate very long and slower outputs."
            },
            new()
            {
                Category = "heavy-reasoning-thinking",
                ModelId = "deepseek/deepseek-r1:free",
                Provider = "DeepSeek",
                MaxContext = "163,840",
                Capabilities = "Deep reasoning and math-intensive tasks",
                Limitations = "Can be slow and verbose; free request limits."
            },
            new()
            {
                Category = "heavy-reasoning-thinking",
                ModelId = "deepseek/deepseek-r1-0528-qwen3-8b:free",
                Provider = "DeepSeek",
                MaxContext = "131,072",
                Capabilities = "Efficient reasoning with long context",
                Limitations = "Smaller model, not top-tier in every benchmark."
            },
            new()
            {
                Category = "coding",
                ModelId = "mistralai/devstral-small:free",
                Provider = "Mistral AI",
                MaxContext = "32,768",
                Capabilities = "Code generation, function calling, dev-agent scenarios",
                Limitations = "Smaller context window than larger coding models."
            },
            new()
            {
                Category = "coding",
                ModelId = "kimi/kimi-dev-72b:free",
                Provider = "Moonshot AI",
                MaxContext = "131,072",
                Capabilities = "Strong coding quality with long-context support",
                Limitations = "Heavy model with potential latency and free-tier caps."
            },
            new()
            {
                Category = "coding",
                ModelId = "qwen/qwen3-coder-480b:free",
                Provider = "Qwen",
                MaxContext = "262k",
                Capabilities = "SOTA-class coding and large repository context handling",
                Limitations = "Very heavy model; free usage can be constrained."
            },
            new()
            {
                Category = "text-only-popular",
                ModelId = "venice/venice-uncensored:free",
                Provider = "Venice",
                MaxContext = "32,768",
                Capabilities = "Text chat with looser safety profile",
                Limitations = "Quality and reliability can vary by task."
            },
            new()
            {
                Category = "text-only-popular",
                ModelId = "sarvamai/sarvam-m:free",
                Provider = "Sarvam AI",
                MaxContext = "32,768",
                Capabilities = "General purpose text chat",
                Limitations = "Limited public benchmark coverage."
            },
            new()
            {
                Category = "text-only-popular",
                ModelId = "tng/deepseek-r1t2-chimera:free",
                Provider = "TNGTech",
                MaxContext = "163,840",
                Capabilities = "Long-context reasoning text tasks",
                Limitations = "Detailed technical profile is less transparent in UI."
            },
            new()
            {
                Category = "text-only-popular",
                ModelId = "tencent/hunyuan-a13b-instruct:free",
                Provider = "Tencent",
                MaxContext = "32,768",
                Capabilities = "General text instruction-following",
                Limitations = "PT-BR quality consistency not clearly documented."
            }
        };

    private static readonly string[] FallbackFreeModels =
        CuratedFreeModels.Select(x => x.ModelId).ToArray();

    public OpenRouterPromptGeneratorService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenRouterPromptGeneratorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public IReadOnlyList<FreeModelCatalogItemViewModel> GetCuratedFreeModels()
    {
        return CuratedFreeModels;
    }

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
            var response = await httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Falha ao buscar modelos OpenRouter. Status: {Status}", response.StatusCode);
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

            var ordered = freeModels
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
                .Take(25)
                .ToList();

            return ordered.Count > 0 ? ordered : FallbackFreeModels;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao consultar modelos free da OpenRouter");
            return FallbackFreeModels;
        }
    }

    public async Task<PromptGenerationResponseViewModel> GenerateAsync(PromptGenerationRequestViewModel request)
    {
        var effectiveApiKey = ResolveApiKey(request.ApiKey);
        if (string.IsNullOrWhiteSpace(effectiveApiKey))
        {
            throw new InvalidOperationException("OpenRouter API key não configurada");
        }

        var systemPrompt = BuildSystemPrompt(request.Purpose);
        var userPrompt = BuildUserPrompt(request);

        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(90);

        var payload = new
        {
            model = request.Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.4,
            max_tokens = 1800
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var completionsUrl = $"{GetBaseUrl().TrimEnd('/')}/chat/completions";
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, completionsUrl)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", effectiveApiKey);
        requestMessage.Headers.TryAddWithoutValidation("HTTP-Referer", _configuration["OpenRouter:Referer"] ?? "https://feradoprompt.local");
        requestMessage.Headers.TryAddWithoutValidation("X-Title", _configuration["OpenRouter:AppTitle"] ?? "Fera do Prompt");
        var response = await httpClient.SendAsync(requestMessage);

        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"OpenRouter error: {response.StatusCode} - {responseBody}");
        }

        var generated = ExtractMessageContent(responseBody);
        if (string.IsNullOrWhiteSpace(generated))
        {
            throw new InvalidOperationException("Resposta da OpenRouter veio vazia");
        }

        return new PromptGenerationResponseViewModel
        {
            Purpose = request.Purpose,
            Model = request.Model,
            GeneratedPrompt = generated.Trim()
        };
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

        // Remove spaces and invisible unicode chars that may come from copy/paste.
        value = Regex.Replace(value, @"\s+", string.Empty);

        // Some users paste only the token body without the OpenRouter prefix.
        // If it looks like a raw hex token, restore the expected prefix.
        if (!value.StartsWith("sk-or-v1-", StringComparison.OrdinalIgnoreCase) &&
            Regex.IsMatch(value, "^[a-fA-F0-9]{64}$"))
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

        var fromConfig = _configuration["OpenRouter:ApiKey"];
        var fromEnvironment = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        var normalizedConfig = NormalizeApiKey(fromConfig);
        if (!string.IsNullOrWhiteSpace(normalizedConfig) && IsLikelyOpenRouterApiKey(normalizedConfig))
        {
            return normalizedConfig;
        }

        var normalizedEnvironment = NormalizeApiKey(fromEnvironment);
        if (!string.IsNullOrWhiteSpace(normalizedEnvironment) && IsLikelyOpenRouterApiKey(normalizedEnvironment))
        {
            return normalizedEnvironment;
        }

        return string.Empty;
    }

    private static bool IsLikelyOpenRouterApiKey(string value)
    {
        return value.StartsWith("sk-or-v1-", StringComparison.OrdinalIgnoreCase);
    }

    private string GetBaseUrl()
    {
        return _configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1";
    }

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
            JsonValueKind.String => double.TryParse(prop.GetString(), out var v) && v == 0d,
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

        if (first.TryGetProperty("message", out var message) &&
            message.ValueKind == JsonValueKind.Object &&
            message.TryGetProperty("content", out var content))
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
            "nano_banana_image" => "Você é um engenheiro de prompt especialista em geração de imagem para Gemini/Nano Banana. Entregue apenas um prompt final pronto para uso.",
            "chatgpt_images" => "Você é um engenheiro de prompt especialista em ChatGPT Images. Entregue apenas um prompt final pronto para uso.",
            "veo3_video_script" => "Você é um roteirista especialista em Google Veo 3. Entregue um roteiro claro com cenas, câmera, ação, áudio e texto.",
            "veo3_video_json" => "Você é um especialista em JSON de geração para Google Veo 3. Entregue apenas JSON válido sem markdown.",
            "grok_imagine_image" => "Você é um engenheiro de prompt especialista em Grok Imagine para imagem estática.",
            "grok_imagine_animate" => "Você é um engenheiro de prompt especialista em Grok Imagine para animar imagens.",
            _ => "Você é um engenheiro de prompt multimodal. Entregue um prompt final objetivo e pronto para uso."
        };
    }

    private static string BuildUserPrompt(PromptGenerationRequestViewModel request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Crie a melhor saída possível para o objetivo selecionado.");
        sb.AppendLine("Regras:");
        sb.AppendLine("1) Seja específico e estruturado.");
        sb.AppendLine("2) Evite texto explicativo sobre o processo.");
        sb.AppendLine("3) Entregue somente a saída final pronta para copiar.");
        sb.AppendLine();
        sb.AppendLine($"Purpose: {request.Purpose}");
        sb.AppendLine($"Brief: {request.Brief}");

        if (!string.IsNullOrWhiteSpace(request.ExtraContext))
        {
            sb.AppendLine($"ExtraContext: {request.ExtraContext}");
        }

        if (!string.IsNullOrWhiteSpace(request.Style))
        {
            sb.AppendLine($"Style: {request.Style}");
        }

        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            sb.AppendLine($"Language: {request.Language}");
        }

        if (!string.IsNullOrWhiteSpace(request.Duration))
        {
            sb.AppendLine($"Duration: {request.Duration}");
        }

        if (!string.IsNullOrWhiteSpace(request.AspectRatio))
        {
            sb.AppendLine($"AspectRatio: {request.AspectRatio}");
        }

        return sb.ToString().Trim();
    }
}
