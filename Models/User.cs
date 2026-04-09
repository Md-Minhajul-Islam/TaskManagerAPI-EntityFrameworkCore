using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models
{
    [Table("Users")] // Explicitly map to "Users" table (Fluent API will override in config)
    public class User : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        [Column("FullName")]
        public string FullName {get; set;} = string.Empty;
        
        [Required]
        [MaxLength(150)]
        [Column("Email")]
        [EmailAddress]      // Validatoin attribute (API level, not DB level)
        public string Email {get; set;} = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column("Role")]
        public string Role {get; set;} = "Member"; // "Admin", "Member"
        
        [Required]
        [Column("IsActive")]
        public bool IsActive {get; set;} = true;
    }
} 
