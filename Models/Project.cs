using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskManagerAPI.Models;

[Table("Projects")]
public class Project : BaseEntity
{
    [Required]
    [MaxLength(150)]
    public string Name {get; set;} = string.Empty;

    [MaxLength(500)]
    public string? Description {get; set;}

    [Required]
    [MaxLength(20)]
    public string Stauts {get; set;} = "Active";

    public bool IsDeleted {get; set;} = false;

    public int TeamId {get; set;} // FK -> Teams.Id
    public int OwnerId {get; set;} // FK -> Users.Id

    public virtual Team                   Team    { get; set; } = null!;
    public virtual User                   Owner   { get; set; } = null!;
    public virtual ICollection<Sprint>    Sprints { get; set; } = new List<Sprint>();
    public virtual ICollection<TaskItem>  Tasks   { get; set; } = new List<TaskItem>();

}