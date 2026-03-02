using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeraPrompt.Api.Models;

[Table("PromptGenerationRecords")]
public class PromptGenerationRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public Guid PublicId { get; set; } = Guid.NewGuid();

    public int? ParentRecordId { get; set; }

    [Required]
    public int Version { get; set; } = 1;

    [Required]
    [MaxLength(80)]
    public string Purpose { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string RequestedModel { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string FinalModel { get; set; } = string.Empty;

    [Required]
    public string Brief { get; set; } = string.Empty;

    public string? ExtraContext { get; set; }

    [MaxLength(200)]
    public string? Language { get; set; }

    [MaxLength(400)]
    public string? Style { get; set; }

    [MaxLength(200)]
    public string? Duration { get; set; }

    [MaxLength(500)]
    public string? AspectRatio { get; set; }

    [Required]
    public bool Success { get; set; }

    public string GeneratedPrompt { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    [Required]
    public bool UsedFallback { get; set; }

    [Required]
    public int Attempts { get; set; }

    [Required]
    public int DurationMs { get; set; }

    [MaxLength(2000)]
    public string? AttemptedModelsJson { get; set; }

    [MaxLength(120)]
    public string? ClientSessionId { get; set; }

    [MaxLength(64)]
    public string? RequestIp { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
