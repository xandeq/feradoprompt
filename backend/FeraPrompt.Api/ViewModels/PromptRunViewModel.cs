using System.ComponentModel.DataAnnotations;

namespace FeraPrompt.Api.ViewModels;

/// <summary>
/// ViewModel para executar um Prompt no n8n
/// </summary>
public class PromptRunViewModel
{
    [Required(ErrorMessage = "O ID do prompt é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "ID do prompt deve ser maior que 0")]
    public int PromptId { get; set; }

    [Required(ErrorMessage = "O input é obrigatório")]
    [MaxLength(2000, ErrorMessage = "O input não pode ter mais de 2000 caracteres")]
    public string Input { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "O nome do modelo não pode ter mais de 50 caracteres")]
    public string Model { get; set; } = "gpt-4o";
}
