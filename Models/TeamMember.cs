using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models;

// This entity has No single Id column
// Instead TeamId + UserId together form the Primary Key (Composite Key)
[Table("TeamMembers")]
public class TeamMember
{
    public int TeamId {get; set;}
    public int UserId {get; set;}

    [Required]
    [MaxLength(20)]
    public string Role {get; set;} = "Member";

    public DateTime JoinedAt {get; set;} = DateTime.UtcNow;

    public Team Team {get; set;} = null!;
    public User User {get; set;} = null!;  
}