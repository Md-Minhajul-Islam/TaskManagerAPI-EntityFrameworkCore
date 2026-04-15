namespace TaskManagerAPI.DTOs.User;

public class AdvancedFeaturesDemo
{
    public bool     Success         { get; set; }
    public string   Feature         { get; set; } = string.Empty;
    public string   Explanation     { get; set; } = string.Empty;
    public string[] Steps           { get; set; } = Array.Empty<string>();
    public object?  Data            { get; set; }
}