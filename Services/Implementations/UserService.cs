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

    // AsNoTracking
    public async Task<IEnumerable<UserResponseDto>> GetAllNoTrackingAsync()
    {
        // AsNoTracking — EF Core loads data but does NOT watch for changes
        // Faster than regular GetAllAsync because no snapshot is created
        var users = await _unitOfWork.Users.GetAllAsNoTrackingAsync();
        return users.Select(MapToResponse);
    }

    // EntityState Demo
    public async Task<EntityStateDemo> GetEntityStateDemoAsync(int id)
    {
        // state 1: Load entity
        // After loading, EF core treacks it as Unchanged
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if(user is null)
            throw new InvalidOperationException($"User {id} not found.");
        
        var stateAfterLoad = _unitOfWork.Users.GetEntityState(user);
        // EntityState = Unchanged ← EF Core is watching but nothing changed

        // State 2: Modify a property
        // Change Tracking detects the modification automatically
        var originalName  = user.FullName;
        user.FullName    += " (modified)";

        var stateAfterChange = _unitOfWork.Users.GetEntityState(user);
        // EntityState = Modified ← EF Core detected the property change

        // Restore original value — we don't actually want to save this
        user.FullName = originalName;

        // ── State 3: Detach entity ─────────────────────────────────────────────
        // After detaching, EF Core no longer tracks this entity
        _unitOfWork.Users.DetachEntity(user);

        var stateAfterDetach = _unitOfWork.Users.GetEntityState(user);
        // EntityState = Detached ← EF Core no longer watches this entity
    
        // state 4: add a new entity (without saving)
            var newUser = new User
        {
            FullName = "Demo User",
            Email    = $"demo_{Guid.NewGuid()}@test.com",
            Role     = "Member"
        };

        await _unitOfWork.Users.AddAsync(newUser);

        var stateAfterAdd = _unitOfWork.Users.GetEntityState(newUser);
        // EntityState = Added ← queued for INSERT but not saved yet

        // Detach the new user so we don't accidentally save it
        _unitOfWork.Users.DetachEntity(newUser);

        // ── State 5: Mark existing entity for deletion (without saving) ────────
        // Re-load the original user since we detached it
        var userForDelete = await _unitOfWork.Users.GetByIdAsync(id);
        if (userForDelete is not null)
        {
            _unitOfWork.Users.Remove(userForDelete);
            var stateAfterRemove = _unitOfWork.Users.GetEntityState(userForDelete);

            // Detach to cancel the delete — we don't actually want to delete
            _unitOfWork.Users.DetachEntity(userForDelete);

            return new EntityStateDemo
            {
                UserId          = id,
                FullName        = originalName,
                StateAfterLoad   = stateAfterLoad.ToString(),
                StateAfterChange = stateAfterChange.ToString(),
                StateAfterDetach = stateAfterDetach.ToString(),
                StateAfterAdd    = stateAfterAdd.ToString(),
                StateAfterRemove = stateAfterRemove.ToString(),
                Explanation      = "EntityState shows what EF Core will do at SaveChanges: " +
                                "Added=INSERT, Modified=UPDATE, Deleted=DELETE, " +
                                "Unchanged=nothing, Detached=not tracked"
            };
        }

        return new EntityStateDemo
        {
            UserId           = id,
            FullName         = originalName,
            StateAfterLoad   = stateAfterLoad.ToString(),
            StateAfterChange = stateAfterChange.ToString(),
            StateAfterDetach = stateAfterDetach.ToString(),
            StateAfterAdd    = stateAfterAdd.ToString(),
            StateAfterRemove = "Could not demonstrate",
            Explanation      = "EntityState lifecycle demonstrated"
        };
    }
}