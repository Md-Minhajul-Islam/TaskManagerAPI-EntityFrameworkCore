using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models;

[Table("Labels")]
public class Label : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string Name {get; set;} = string.Empty;

    [Required]
    [MaxLength(7)]
    public string Color {get; set;} = "#000000";

    public virtual ICollection<TaskLabel> TaskLabels {get; set;} = new List<TaskLabel>();
}