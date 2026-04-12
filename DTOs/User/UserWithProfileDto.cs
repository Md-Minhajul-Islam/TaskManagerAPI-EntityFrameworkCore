namespace TaskManagerAPI.DTOs.User;

public class UserWithProfileDto
{
    public int     Id       { get; set; }
    public string  FullName { get; set; } = string.Empty;
    public string  Email    { get; set; } = string.Empty;
    public string  LoadingStrategy { get; set; } = string.Empty;
    public ProfileDto? Profile { get; set; }
}

public class ProfileDto
{
    public string  Bio       { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? GitHubUrl { get; set; }
}