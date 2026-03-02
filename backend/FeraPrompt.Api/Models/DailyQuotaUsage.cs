using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeraPrompt.Api.Models;

[Table("DailyQuotaUsages")]
public class DailyQuotaUsage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public DateTime DateUtc { get; set; }

    [Required]
    [MaxLength(64)]
    public string IpAddress { get; set; } = string.Empty;

    [Required]
    public int Count { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
