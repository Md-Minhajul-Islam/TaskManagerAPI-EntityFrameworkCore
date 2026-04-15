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

    [Required]
    [Column("IsDeleted")]
    public bool IsDeleted {get; set;} = false;

    // Concurrency Token — EF Core uses this to detect conflicting updates
    // SQL Server automatically updates this value on every UPDATE
    // If two users try to update the same row simultaneously:
    //   User A reads RowVersion = 1, updates → RowVersion becomes 2
    //   User B reads RowVersion = 1, tries to update → fails! RowVersion is now 2
    [Timestamp]
    public byte[] RowVersion {get; set;} = Array.Empty<byte>();

    public virtual UserProfile?              Profile        { get; set; }
    public virtual ICollection<TeamMember>  TeamMembers    { get; set; } = new List<TeamMember>();
    public virtual ICollection<Project>     OwnedProjects  { get; set; } = new List<Project>();
    public virtual ICollection<TaskItem>    ReportedTasks  { get; set; } = new List<TaskItem>();
    public virtual ICollection<TaskItem>    AssignedTasks  { get; set; } = new List<TaskItem>();
    public virtual ICollection<Comment>     Comments       { get; set; } = new List<Comment>();
}