using System.Text;
using System.Text.Json;
using FeraPrompt.Api.Data;
using FeraPrompt.Api.Models;
using FeraPrompt.Api.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FeraPrompt.Api.Services;

/// <summary>
/// Implementação do serviço de gerenciamento de Prompts com integração n8n
/// </summary>
public class PromptService : IPromptService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PromptService> _logger;
    private readonly string _environment;

    public PromptService(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PromptService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
    }

    /// <summary>
    /// Cria um novo Prompt no banco de dados
    /// </summary>
    public async Task<Prompt> CreatePromptAsync(PromptCreateViewModel model)
    {
        try
        {
            var prompt = new Prompt
            {
                Title = model.Title,
                Body = model.Body,
                Model = model.Model,
                CreatedAt = DateTime.UtcNow
            };

            _context.Prompts.Add(prompt);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Prompt criado com sucesso. ID: {PromptId}", prompt.Id);
            return prompt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar prompt");
            throw;
        }
    }

    /// <summary>
    /// Executa um Prompt enviando para o webhook do n8n e salvando histórico
    /// </summary>
    public async Task<PromptResponseViewModel> ExecutePromptAsync(PromptRunViewModel model)
    {
        try
        {
            // Busca o prompt no banco
            var prompt = await _context.Prompts.FindAsync(model.PromptId);
            if (prompt == null)
            {
                throw new KeyNotFoundException($"Prompt com ID {model.PromptId} não encontrado");
            }

            // Determina qual webhook usar com base no ambiente
            var webhookUrl = _environment.Equals("Development", StringComparison.OrdinalIgnoreCase)
                ? _configuration["N8n:WEBHOOK_TEST_URL"]
                : _configuration["N8n:WEBHOOK_PRODUCTION_URL"];

            if (string.IsNullOrEmpty(webhookUrl))
            {
                throw new InvalidOperationException("URL do webhook n8n não configurada");
            }

            // Monta o payload para o n8n
            var payload = new
            {
                promptId = model.PromptId,
                model = model.Model,
                input = model.Input,
                promptBody = prompt.Body
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            _logger.LogInformation("Enviando requisição para n8n. Environment: {Environment}", _environment);

            // Envia para o n8n
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(120); // 2 minutos de timeout

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(webhookUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erro ao chamar webhook n8n. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Erro ao executar prompt no n8n: {response.StatusCode}");
            }

            // Lê a resposta do n8n
            var responseJson = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseJson))
            {
                 throw new InvalidOperationException("O n8n retornou um corpo de resposta vazio.");
            }

            // Tenta deserializar como um dicionário genérico para inspecionar os campos
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(responseJson);
            string output = string.Empty;

            // Estratégia de fallback para encontrar o output
            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                // 1. Tenta encontrar "output" ou "Output"
                if (jsonElement.TryGetProperty("output", out var outputProp) ||
                    jsonElement.TryGetProperty("Output", out outputProp))
                {
                    output = outputProp.ToString();
                }
                // 2. Se não encontrar, tenta usar "optimized_prompt" (baseado no log de erro)
                else if (jsonElement.TryGetProperty("optimized_prompt", out var optimizedProp))
                {
                    output = optimizedProp.ToString();
                }
                // 3. Se não, tenta serializar o objeto "analysis" se existir
                else if (jsonElement.TryGetProperty("analysis", out var analysisProp))
                {
                    output = analysisProp.ToString();
                }
                // 4. Último caso: serializa o JSON inteiro recebido
                else
                {
                    output = responseJson;
                }
            }
            else
            {
                output = responseJson;
            }

            if (string.IsNullOrEmpty(output))
            {
                 _logger.LogWarning("Não foi possível extrair um output válido. Resposta completa: {ResponseJson}", responseJson);
                throw new InvalidOperationException($"O n8n retornou sucesso, mas não foi possível extrair o resultado. Recebido: {responseJson}");
            }

            // Salva no histórico
            var history = new PromptHistory
            {
                PromptId = model.PromptId,
                Input = model.Input,
                Output = output,
                ModelUsed = model.Model,
                ExecutedAt = DateTime.UtcNow
            };

            _context.PromptHistories.Add(history);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Prompt executado com sucesso. HistoryId: {HistoryId}", history.Id);

            // Retorna o ViewModel de resposta
            return new PromptResponseViewModel
            {
                PromptId = model.PromptId,
                Input = model.Input,
                Output = output,
                ModelUsed = model.Model,
                ExecutedAt = history.ExecutedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar prompt {PromptId}", model.PromptId);
            throw;
        }
    }

    /// <summary>
    /// Retorna todos os Prompts cadastrados
    /// </summary>
    public async Task<IEnumerable<Prompt>> GetAllPromptsAsync()
    {
        try
        {
            return await _context.Prompts
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar prompts");
            throw;
        }
    }

    /// <summary>
    /// Busca um Prompt específico por ID
    /// </summary>
    public async Task<Prompt?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Prompts
                .Include(p => p.PromptHistories)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar prompt {PromptId}", id);
            throw;
        }
    }

    /// <summary>
    /// Remove um Prompt do banco de dados
    /// </summary>
    public async Task DeletePromptAsync(int id)
    {
        try
        {
            var prompt = await _context.Prompts.FindAsync(id);
            if (prompt == null)
            {
                throw new KeyNotFoundException($"Prompt com ID {id} não encontrado");
            }

            _context.Prompts.Remove(prompt);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Prompt {PromptId} removido com sucesso", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar prompt {PromptId}", id);
            throw;
        }
    }

    /// <summary>
    /// Classe interna para deserializar resposta do n8n (Removida pois agora usamos JsonElement dinâmico)
    /// </summary>
    // private class N8nWebhookResponse ...
}
