using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models;

[Table("Sprints")]
public class Sprint : BaseEntity
{

    [Required]
    [MaxLength(150)]
    public string Name {get; set;} = string.Empty;

    [Required]
    [MaxLength(300)]
    public string? Goal {get; set;}

    public DateTime StartDate {get; set;}
    public DateTime EndDate {get; set;}

    public int ProjectId {get; set;}

    public virtual Project               Project { get; set; } = null!;
    public virtual ICollection<TaskItem> Tasks   { get; set; } = new List<TaskItem>();
}