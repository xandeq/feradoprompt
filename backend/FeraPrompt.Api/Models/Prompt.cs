using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeraPrompt.Api.Models;

/// <summary>
/// Representa um prompt template no sistema
/// </summary>
[Table("Prompts")]
public class Prompt
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Model { get; set; } = string.Empty; // Ex: "gpt-5", "claude-sonnet"

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    // Navegação: Um Prompt pode ter muitos históricos
    public virtual ICollection<PromptHistory> PromptHistories { get; set; } = new List<PromptHistory>();
}
