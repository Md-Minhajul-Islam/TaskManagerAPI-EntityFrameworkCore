using System.Transactions;
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

    public async Task<object> GetPerformanceDemoAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsNoTrackingAsync();

        var list = users.ToList();

        return new
        {
            TotalUsers   = list.Count,
            ActiveUsers  = list.Count(u => u.IsActive),
            ByRole       = list
                            .GroupBy(u => u.Role)
                            .Select(g => new { Role = g.Key, Count = g.Count() }),
            Optimizations = new[]
            {
                "AsNoTracking — no change tracking snapshot created",
                "Compiled queries on GetByEmail, GetActiveUsers, GetByRole",
                "Composite index on IsActive + Role columns",
                "Filtered index on IsActive = 1 — smaller index",
                "AsSplitQuery on GetWithAllRelatedAsync — no cartesian explosion"
            }
        };
    }

    // Transaction: Bulk create users atomically 
    // All users are created or NONE are created
    // If any one fails - All rolllback
    public async Task<TransactionDemo> BulkCreateUsersAsync(
        List<CreateUserDto> dtos
    )
    {
        // Step 1 - begin transaction
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        var steps = new List<string>();
        steps.Add("Transaction started");

        try
        {
            var createdUsers = new List<UserResponseDto>();
            
            foreach(var dto in dtos)
            {
                var isUnique = await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email);
                if (!isUnique)
                {
                    throw new InvalidOperationException(
                        $"Email '{dto.Email}' already exists — rolling back ALL users"
                    );
                }
                var user = new User
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    Role = dto.Role
                };
                await _unitOfWork.Users.AddAsync(user);
                steps.Add($"User '{dto.FullName}' queued for INSERT");

                // SaveChangesAsync inside transaction — changes are staged
                // NOT yet committed to DB — still reversible
                await _unitOfWork.SaveChangesAsync();
                steps.Add($"User '{dto.FullName}' saved (not yet committed)");

                createdUsers.Add(MapToResponse(user));

            }
            // Step 2 - commit - makes all changes permanent
            await _unitOfWork.CommitAsync(transaction);
            steps.Add("Transactions Commited - all users saved permanently");
            
            return new TransactionDemo
            {
                Success  = true,
                Scenario = "Bulk create users — atomic transaction",
                Outcome  = $"All {dtos.Count} users created successfully",
                Steps    = steps.ToArray(),
                Data     = createdUsers
            };
        }
        catch(Exception ex)
        {
            // Step - 3 - Rollback - undoes all changes on any failure
            await _unitOfWork.RollbackAsync(transaction);
            steps.Add($"ERROR: {ex.Message}");
            steps.Add("Transaction ROLLED BACK — no users saved");

            return new TransactionDemo
            {
                Success  = false,
                Scenario = "Bulk create users — atomic transaction",
                Outcome  = "Transaction rolled back — zero users created",
                Steps    = steps.ToArray(),
                Data     = null
            };
        }    
    }

    // ── TRANSACTION: Demonstrate intentional rollback ──────────────────────────
    // Creates two users then intentionally rolls back
    // Shows that NOTHING was saved despite SaveChangesAsync being called
    public async Task<TransactionDemo> TransactionWithRollbackDemoAsync()
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        var steps = new List<string>();
        steps.Add("Transaction started");

        try
        {
            // Create user 1
            var user1 = new User
            {
                FullName = "Transaction Test User 1",
                Email    = $"txtest1_{Guid.NewGuid()}@demo.com",
                Role     = "Member"
            };
            await _unitOfWork.Users.AddAsync(user1);
            await _unitOfWork.SaveChangesAsync();
            steps.Add($"User 1 saved (Id={user1.Id}) — inside transaction, not committed");

            // Create user 2
            var user2 = new User
            {
                FullName = "Transaction Test User 2",
                Email    = $"txtest2_{Guid.NewGuid()}@demo.com",
                Role     = "Member"
            };
            await _unitOfWork.Users.AddAsync(user2);
            await _unitOfWork.SaveChangesAsync();
            steps.Add($"User 2 saved (Id={user2.Id}) — inside transaction, not committed");

            // Intentionally throw to demonstrate rollback
            steps.Add("Intentionally throwing exception to demonstrate rollback...");
            throw new InvalidOperationException("Simulated failure — rollback demo");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(transaction);
            steps.Add($"Exception: {ex.Message}");
            steps.Add("Transaction ROLLED BACK — both users are gone from DB ❌");
            steps.Add("Check DB — you will NOT find these users");

            return new TransactionDemo
            {
                Success  = false,
                Scenario = "Intentional rollback demo",
                Outcome  = "Both users were rolled back — DB unchanged",
                Steps    = steps.ToArray(),
                Data     = null
            };
        }
    }

    // ── TRANSACTION: Savepoint demo ────────────────────────────────────────────
    // Savepoints let you roll back PART of a transaction
    // without losing ALL changes
    public async Task<TransactionDemo> SavepointDemoAsync()
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        var steps = new List<string>();
        var created = new List<string>();

        steps.Add("Transaction Started");

        try
        {
            // Create user 1 — this will be KEPT
            var user1 = new User
            {
                FullName = "Savepoint User 1 (kept)",
                Email    = $"sp1_{Guid.NewGuid()}@demo.com",
                Role     = "Member"
            };
            await _unitOfWork.Users.AddAsync(user1);
            await _unitOfWork.SaveChangesAsync();
            steps.Add($"User 1 saved — Id={user1.Id}");
            created.Add(user1.FullName);

            // Create a savepoint AFTER user 1
            // "Remember this point — I may want to roll back to here"
            await transaction.CreateSavepointAsync("AfterUser1");
            steps.Add("Savepoint 'AfterUser1' created");

            // Create user 2 — this will be ROLLED BACK to savepoint
            var user2 = new User
            {
                FullName = "Savepoint User 2 (rolled back)",
                Email    = $"sp2_{Guid.NewGuid()}@demo.com",
                Role     = "Member"
            };
            await _unitOfWork.Users.AddAsync(user2);
            await _unitOfWork.SaveChangesAsync();
            steps.Add($"User 2 saved — Id={user2.Id}");
        
            // Simulate a problem with user 2
            steps.Add("Problem detected with User 2 — rolling back to savepoint...");
        
            // Roll back to savepoint — ONLY user 2 is undone
            // User 1 is still intact!
            await transaction.RollbackToSavepointAsync("AfterUser1");
            steps.Add("Rolled back to 'AfterUser1' — User 2 is gone, User 1 is safe");

            // Commit — only user 1 is committed
            await _unitOfWork.CommitAsync(transaction);
            steps.Add("Transaction COMMITTED — only User 1 saved");

            return new TransactionDemo
            {
                Success  = true,
                Scenario = "Savepoint demo — partial rollback",
                Outcome  = "User 1 committed, User 2 rolled back to savepoint",
                Steps    = steps.ToArray(),
                Data     = new { CommittedUsers = created }
            };
        
        }
        catch(Exception ex)
        {
            await _unitOfWork.RollbackAsync(transaction);
            steps.Add($"Unexpected error: {ex.Message}");
            steps.Add("Full rollback — nothing saved");

            return new TransactionDemo
            {
                Success  = false,
                Scenario = "Savepoint demo",
                Outcome  = "Unexpected error — full rollback",
                Steps    = steps.ToArray()
            };
        }
    }

     // ── FromSqlRaw: get by role ────────────────────────────────────────────────
    public async Task<IEnumerable<UserResponseDto>> GetByRoleRawSqlAsync(string role)
    {
        var users = await _unitOfWork.Users.GetByRoleRawSqlAsync(role);
        return users.Select(MapToResponse);
    }

    // ── FromSqlRaw: get by email ───────────────────────────────────────────────
    public async Task<UserResponseDto?> GetByEmailRawSqlAsync(string email)
    {
        var user = await _unitOfWork.Users.GetByEmailRawSqlAsync(email);
        return user is null ? null : MapToResponse(user);
    }

    // ── ExecuteSqlRaw: deactivate single user ──────────────────────────────────
    public async Task<RawSqlDemo> DeactivateUserRawSqlAsync(int userId)
    {
        var rows = await _unitOfWork.Users.DeactivateUserRawSqlAsync(userId);

        return new RawSqlDemo
        {
            Success      = rows > 0,
            Method       = "ExecuteSqlRaw",
            SqlExecuted  = $"UPDATE Users SET IsActive = 0 WHERE Id = {userId}",
            RowsAffected = rows,
            Message      = rows > 0
                            ? $"User {userId} deactivated"
                            : $"User {userId} not found"
        };
    }

    // ── ExecuteSqlRaw: bulk deactivate by role ─────────────────────────────────
    public async Task<RawSqlDemo> BulkDeactivateByRoleAsync(string role)
    {
        var rows = await _unitOfWork.Users.BulkDeactivateByRoleAsync(role);

        return new RawSqlDemo
        {
            Success      = true,
            Method       = "ExecuteSqlRaw (Bulk)",
            SqlExecuted  = $"UPDATE Users SET IsActive = 0 WHERE Role = '{role}'",
            RowsAffected = rows,
            Message      = $"{rows} users with role '{role}' deactivated"
        };
    }

    // ── Stored Procedure: get active users by role ─────────────────────────────
    public async Task<IEnumerable<UserResponseDto>> GetActiveUsersByRoleSpAsync(
        string role)
    {
        var users = await _unitOfWork.Users.GetActiveUsersByRoleSpAsync(role);
        return users.Select(MapToResponse);
    }

    // ── Stored Procedure: update user role ────────────────────────────────────
    public async Task<RawSqlDemo> UpdateUserRoleSpAsync(int userId, string newRole)
    {
        var rows = await _unitOfWork.Users.UpdateUserRoleSpAsync(userId, newRole);

        return new RawSqlDemo
        {
            Success      = rows > 0,
            Method       = "ExecuteSqlRaw (Stored Procedure)",
            SqlExecuted  = $"EXEC sp_UpdateUserRole {userId}, '{newRole}'",
            RowsAffected = rows,
            Message      = rows > 0
                            ? $"User {userId} role updated to '{newRole}'"
                            : $"User {userId} not found"
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