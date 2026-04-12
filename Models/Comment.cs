using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerAPI.Models;

[Table("Comments")]
public class Comment : BaseEntity
{
    [Required]
    [MaxLength(2000)]
    public string Content {get; set;} = string.Empty;

    public bool IsDeleted {get; set;} = false;

    public int TaskId {get; set;} // FK -> TaskItems.Id
    public int AuthorId {get; set;} // FK -> Users.Id

    public virtual TaskItem Task   { get; set; } = null!;
    public virtual User     Author { get; set; } = null!;
}