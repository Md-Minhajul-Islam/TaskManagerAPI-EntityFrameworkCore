using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models;

[Table("Users")]                        // ← Map to exact table name
public class User : BaseEntity
{
    [Required]                          // ← NOT NULL in DB
    [MaxLength(100)]                    // ← NVARCHAR(100) in DB
    [Column("FullName")]                // ← Explicit column name
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Column("Email")]
    [EmailAddress]                      // ← Format validation (API level)
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("Role")]
    public string Role { get; set; } = "Member";

    [Required]
    [Column("IsActive")]
    public bool IsActive { get; set; } = true;
}