using System.ComponentModel.DataAnnotations;

namespace FeraPrompt.Api.ViewModels;

public class PromptGenerationRequestViewModel
{
    [Required]
    [MaxLength(80)]
    public string Purpose { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [MaxLength(6000)]
    public string Brief { get; set; } = string.Empty;

    [MaxLength(3000)]
    public string? ExtraContext { get; set; }

    [MaxLength(200)]
    public string? Language { get; set; }

    [MaxLength(400)]
    public string? Style { get; set; }

    [MaxLength(200)]
    public string? Duration { get; set; }

    [MaxLength(500)]
    public string? AspectRatio { get; set; }

    [MaxLength(500)]
    public string? ApiKey { get; set; }
}
