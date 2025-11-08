using System.ComponentModel.DataAnnotations;

namespace FeraPrompt.Api.ViewModels;

/// <summary>
/// ViewModel para criação de um novo Prompt
/// </summary>
public class PromptCreateViewModel
{
    [Required(ErrorMessage = "O título é obrigatório")]
    [MaxLength(200, ErrorMessage = "O título não pode ter mais de 200 caracteres")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "O corpo do prompt é obrigatório")]
    [MaxLength(5000, ErrorMessage = "O corpo não pode ter mais de 5000 caracteres")]
    public string Body { get; set; } = string.Empty;

    [Required(ErrorMessage = "O modelo é obrigatório")]
    [MaxLength(50, ErrorMessage = "O nome do modelo não pode ter mais de 50 caracteres")]
    public string Model { get; set; } = "gpt-4o";
}
