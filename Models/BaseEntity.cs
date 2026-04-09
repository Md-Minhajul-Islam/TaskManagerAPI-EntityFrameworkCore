using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models;

public abstract class BaseEntity
{
    [Key]                               // ← Marks as Primary Key
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // ← Auto-increment
    public int Id { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}