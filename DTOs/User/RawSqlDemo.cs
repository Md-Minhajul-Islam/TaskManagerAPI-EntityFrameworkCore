namespace TaskManagerAPI.DTOs.User;

public class RawSqlDemo
{
    public bool   Success      { get; set; }
    public string Method       { get; set; } = string.Empty; // FromSqlRaw / ExecuteSqlRaw / SP
    public string SqlExecuted  { get; set; } = string.Empty; // the SQL that ran
    public int    RowsAffected { get; set; }
    public string Message      { get; set; } = string.Empty;
}