using System.ComponentModel.DataAnnotations;

namespace TaskManagerAPI.DTOs.Task;

public class CreateTaskDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public string Priority { get; set; } = "Medium";

    [Required]
    public int ProjectId  { get; set; }

    [Required]
    public int ReporterId { get; set; }

    public int? AssigneeId { get; set; }
    public int? SprintId   { get; set; }
}