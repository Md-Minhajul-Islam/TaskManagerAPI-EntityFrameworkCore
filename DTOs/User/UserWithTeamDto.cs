namespace TaskManagerAPI.DTOs.User;

public class UserWithTeamsDto
{
    public int    Id       { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public IEnumerable<TeamDto> Teams { get; set; } = new List<TeamDto>();
}

public class TeamDto
{
    public int      Id       { get; set; }
    public string   Name     { get; set; } = string.Empty;
    public string   Role     { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}