using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models;

[Table("Teams")]
public class Team : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name {get; set;} = string.Empty;

    [MaxLength(500)]
    public string? Description {get; set;}

    public ICollection<Project> Projects {get; set;} = new List<Project>();

    public ICollection<TeamMember> TeamMembers {get; set;} = new List<TeamMember>();
}