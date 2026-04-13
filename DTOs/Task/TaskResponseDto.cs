namespace TaskManagerAPI.DTOs.Task;

public class TaskResponseDto
{
    public int      Id          { get; set; }
    public string   Title       { get; set; } = string.Empty;
    public string?  Description { get; set; }
    public string   Status      { get; set; } = string.Empty;
    public string   Priority    { get; set; } = string.Empty;
    public int      ProjectId   { get; set; }
    public int      ReporterId  { get; set; }
    public int?     AssigneeId  { get; set; }
    public int?     SprintId    { get; set; }
    public DateTime CreatedAt   { get; set; }
}