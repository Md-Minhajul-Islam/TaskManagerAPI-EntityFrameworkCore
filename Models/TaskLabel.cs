using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models;

// Junction table for Many-to-Many between TaskItem and Label
[Table("TaskLabels")]
public class TaskLabel
{
    public int TaskId {get; set;}
    public int LabelId {get; set;}

    public TaskItem Task  {get; set;} = null!;
    public Label Label {get; set;} = null!;
}