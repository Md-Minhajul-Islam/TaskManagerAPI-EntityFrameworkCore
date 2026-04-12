using TaskManagerAPI.DTOs.User;
using TaskManagerAPI.Models;
using TaskManagerAPI.Services.Interfaces;
using TaskManagerAPI.UnitOfWork;

namespace TaskManagerAPI.Services.Implementations;

// Service layer contains all business logic
// It uses UnitOfWork to access repositories
// and calls SaveChangesAsync once per operation
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return users.Select(MapToResponse);
    }

    public async Task<UserResponseDto?> GetByIdAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        return user is null ? null : MapToResponse(user);
    }

    public async Task<UserResponseDto> CreateAsync(CreateUserDto dto)
    {
        var isUnique = await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email);
        if(!isUnique)
            throw new InvalidOperationException($"Email '{dto.Email}' is already taken");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Role = dto.Role
        };

        await _unitOfWork.Users.AddAsync(user);

        // SaveChangesAsync — EF Core generates INSERT SQL
        // EntityState goes: Detached → Added → Unchanged (after save)
        await _unitOfWork.SaveChangesAsync();
        return MapToResponse(user);

    }


    public async Task<UserResponseDto?> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null) return null;

        // Change Tracking detects these modifications automatically
        // EntityState goes: Unchanged → Modified (after property change)
        user.FullName = dto.FullName;
        user.Role     = dto.Role;
        user.IsActive = dto.IsActive;

        _unitOfWork.Users.Update(user);

        // SaveChangesAsync — EF Core generates UPDATE SQL
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(user);
    }


    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null) return false;

        // EntityState goes: Unchanged → Deleted
        _unitOfWork.Users.Remove(user);

        // SaveChangesAsync — EF Core generates DELETE SQL
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    // ── Eager Loading — Include() ──────────────────────────────────────────
    public async Task<UserWithProfileDto?> GetWithProfileAsync(int id)
    {
        var user = await _unitOfWork.Users.GetWithProfileAsync(id);
        if (user is null) return null;

        return new UserWithProfileDto
        {
            Id              = user.Id,
            FullName        = user.FullName,
            Email           = user.Email,
            LoadingStrategy = "Eager — loaded with Include() in ONE SQL query",
            Profile         = user.Profile is null ? null : new ProfileDto
            {
                Bio       = user.Profile.Bio,
                AvatarUrl = user.Profile.AvatarUrl,
                GitHubUrl = user.Profile.GitHubUrl
            }
        };
    }

    // ── Eager Loading — Include() + ThenInclude() ──────────────────────────
    public async Task<UserWithTeamsDto?> GetWithTeamsAsync(int id)
    {
        var user = await _unitOfWork.Users.GetWithTeamsAsync(id);
        if (user is null) return null;

        return new UserWithTeamsDto
        {
            Id       = user.Id,
            FullName = user.FullName,
            Email    = user.Email,
            Teams    = user.TeamMembers.Select(tm => new TeamDto
            {
                Id       = tm.Team.Id,
                Name     = tm.Team.Name,
                Role     = tm.Role,
                JoinedAt = tm.JoinedAt
            })
        };
    }

    // ── Explicit Loading — Entry().Reference/Collection().LoadAsync() ───────
    public async Task<UserWithProfileDto?> GetWithExplicitLoadAsync(int id)
    {
        // Step 1 — load User with NO related data
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null) return null;

        // Step 2 — explicitly load each relation with separate SQL queries
        await _unitOfWork.Users.LoadProfileAsync(user);
        await _unitOfWork.Users.LoadTeamsAsync(user);

        return new UserWithProfileDto
        {
            Id              = user.Id,
            FullName        = user.FullName,
            Email           = user.Email,
            LoadingStrategy = "Explicit — each relation loaded with a separate SQL query",
            Profile         = user.Profile is null ? null : new ProfileDto
            {
                Bio       = user.Profile.Bio,
                AvatarUrl = user.Profile.AvatarUrl,
                GitHubUrl = user.Profile.GitHubUrl
            }
        };
    }

    // ── Lazy Loading — access navigation property directly ─────────────────
    public async Task<UserWithProfileDto?> GetWithLazyLoadAsync(int id)
    {
        // Load User with NO Include()
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null) return null;

        // EF Core proxy fires SQL automatically when Profile is accessed
        var profile = user.Profile;  // ← SQL fires here automatically!

        return new UserWithProfileDto
        {
            Id              = user.Id,
            FullName        = user.FullName,
            Email           = user.Email,
            LoadingStrategy = "Lazy — SQL fired automatically when Profile was accessed",
            Profile         = profile is null ? null : new ProfileDto
            {
                Bio       = profile.Bio,
                AvatarUrl = profile.AvatarUrl,
                GitHubUrl = profile.GitHubUrl
            }
        };
    }

    // Private mapper - AutoMapper replaces this in Step 14
    private static UserResponseDto MapToResponse(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}