namespace TaskManagerAPI.DTOs.Task;

public class UpdateTaskDto
{
    public string  Title       { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string  Status      { get; set; } = string.Empty;
    public string  Priority    { get; set; } = string.Empty;
    public int?    AssigneeId  { get; set; }
    public int?    SprintId    { get; set; }
}