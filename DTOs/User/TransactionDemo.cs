namespace TaskManagerAPI.DTOs.User;

public class TransactionDemo
{
    public bool     Success     { get; set; }
    public string   Scenario    { get; set; } = string.Empty;
    public string   Outcome     { get; set; } = string.Empty;
    public string[] Steps       { get; set; } = Array.Empty<string>();
    public object?  Data        { get; set; }
}