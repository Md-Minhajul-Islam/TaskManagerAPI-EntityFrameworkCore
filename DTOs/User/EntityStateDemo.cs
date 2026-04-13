namespace TaskManagerAPI.DTOs.User;

public class EntityStateDemo
{
    public int    UserId          { get; set; }
    public string FullName        { get; set; } = string.Empty;

    // Shows state at each point in the lifecycle
    public string StateAfterLoad   { get; set; } = string.Empty; // Unchanged
    public string StateAfterChange { get; set; } = string.Empty; // Modified
    public string StateAfterDetach { get; set; } = string.Empty; // Detached
    public string StateAfterAdd    { get; set; } = string.Empty; // Added
    public string StateAfterRemove { get; set; } = string.Empty; // Deleted

    public string Explanation { get; set; } = string.Empty;
}