using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeraPrompt.Api.Models;

[Table("ModelPerformanceStats")]
public class ModelPerformanceStat
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(80)]
    public string Purpose { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string ModelId { get; set; } = string.Empty;

    [Required]
    public int TotalRequests { get; set; }

    [Required]
    public int SuccessCount { get; set; }

    [Required]
    public int FailureCount { get; set; }

    [Required]
    public long TotalLatencyMs { get; set; }

    [Required]
    public int ConsecutiveFailures { get; set; }

    public int? LastStatusCode { get; set; }

    [MaxLength(500)]
    public string? LastError { get; set; }

    [Required]
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
}
