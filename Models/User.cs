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


    public UserProfile? Profile {get; set;}
    public ICollection<TeamMember> TeamMembers {get; set;} = new List<TeamMember>();
    public ICollection<Project> OwnedProjects {get; set;} = new List<Project>();
    public ICollection<TaskItem> ReportedTasks {get; set;} = new List<TaskItem>();
    public ICollection<TaskItem> AssignedTasks {get; set;} = new List<TaskItem>();
    public ICollection<Comment> Comments {get; set;} = new List<Comment>();

}