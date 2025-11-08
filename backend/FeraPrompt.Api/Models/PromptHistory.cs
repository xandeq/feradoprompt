using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeraPrompt.Api.Models;

/// <summary>
/// Representa o histórico de execuções de um prompt
/// </summary>
[Table("PromptHistories")]
public class PromptHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int PromptId { get; set; }

    [Required]
    public string Input { get; set; } = string.Empty;

    [Required]
    public string Output { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ModelUsed { get; set; } = string.Empty; // Modelo usado na execução

    [Required]
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    // Navegação: Cada histórico pertence a um Prompt
    [ForeignKey(nameof(PromptId))]
    public virtual Prompt? Prompt { get; set; }
}
