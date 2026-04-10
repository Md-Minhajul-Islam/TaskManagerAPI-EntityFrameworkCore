using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models;

[Table("TaskItems")]
public class TaskItem : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string Title {get; set;} = string.Empty;

    [MaxLength(2000)]
    public string? Description {get; set;}

    [Required]
    [MaxLength(20)]
    public string Status {get; set;} = "Todo"; // Todo, InProgress, Done

    [Required]
    [MaxLength(20)]
    public string Priority {get; set;} = "Medium";

    public bool IsDeleted {get; set;} = false;

    public int ProjectId {get; set;}
    public int ReporterId {get; set;} // FK -> Users.Id
    public int? AssigneeId {get; set;} // FK -> Users.Id
    public int? SprintId {get; set;}

    public Project Project {get; set;} = null!;

    public User Reporter {get; set;} = null!;

    public User? Assignee {get; set;}

    public Sprint? Sprint {get; set;}

    public ICollection<Comment> Comments {get; set;} = new List<Comment>();

    public ICollection<TaskLabel> TaskLabels {get; set;} = new List<TaskLabel>();
}