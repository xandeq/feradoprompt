using FeraPrompt.Api.Models;
using FeraPrompt.Api.ViewModels;

namespace FeraPrompt.Api.Services;

/// <summary>
/// Interface para serviço de gerenciamento de Prompts com integração n8n
/// </summary>
public interface IPromptService
{
    /// <summary>
    /// Cria um novo Prompt no banco de dados
    /// </summary>
    Task<Prompt> CreatePromptAsync(PromptCreateViewModel model);

    /// <summary>
    /// Executa um Prompt enviando para o webhook do n8n e salvando histórico
    /// </summary>
    Task<PromptResponseViewModel> ExecutePromptAsync(PromptRunViewModel model);

    /// <summary>
    /// Retorna todos os Prompts cadastrados
    /// </summary>
    Task<IEnumerable<Prompt>> GetAllPromptsAsync();

    /// <summary>
    /// Busca um Prompt específico por ID com histórico
    /// </summary>
    Task<Prompt?> GetByIdAsync(int id);

    /// <summary>
    /// Remove um Prompt do banco de dados
    /// </summary>
    Task DeletePromptAsync(int id);
}
