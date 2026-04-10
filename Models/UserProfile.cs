using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models;

[Table("UserProfiles")]
public class UserProfile : BaseEntity
{
    [Required]
    public string Bio {get; set;} = string.Empty;

    [MaxLength(255)]
    public string? AvatarUrl {get; set;}

    [MaxLength(255)]
    public string? GitHubUrl {get; set;}

    // Foreign Key property - links UserProfile to User
    // Every UserProfile MUST belong to exactly one user
    public int UserId {get; set;}
    
    public User User {get; set;}= null!;
}