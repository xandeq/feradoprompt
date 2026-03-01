using System.ComponentModel.DataAnnotations;

namespace FeraPrompt.Api.ViewModels;

public class FreeModelsRequestViewModel
{
    [MaxLength(500)]
    public string? ApiKey { get; set; }
}
