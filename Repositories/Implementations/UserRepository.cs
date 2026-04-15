using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;
using TaskManagerAPI.Models;
using TaskManagerAPI.Repositories.Interfaces;

namespace TaskManagerAPI.Repositories.Implementations;

public class UserRepository : Repository<User>, IUserRepository
{

    // Compiled Queries
    // Compiled ONCE at startup - EF Core skips LINQ -> SQL translation
    // on every subsequent call. Best for high-frequency queries.

    // Compiled: get user by email
    private static readonly Func<AppDbContext, string, Task<User?>>
        GetByEmailCompiled = EF.CompileAsyncQuery((AppDbContext ctx, string email) =>
            ctx.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower()));

    
    // Compiled: get all active users ordered by name
    private static readonly Func<AppDbContext, IAsyncEnumerable<User>>
        GetActiveUsersCompiled =
            EF.CompileAsyncQuery((AppDbContext ctx) =>
                ctx.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FullName)
                    .AsNoTracking());

    // Compiled: get users by role
    private static readonly Func<AppDbContext, string, IAsyncEnumerable<User>>
        GetByRoleCompiled =
            EF.CompileAsyncQuery((AppDbContext ctx, string role) =>
                ctx.Users
                    .Where(u => u.Role == role && u.IsActive)
                    .OrderBy(u => u.FullName)
                    .AsNoTracking());


    public UserRepository(AppDbContext context) : base(context)
    {
        
    }

    public async Task<User?> GetByEmailAsync(string email)
        => await GetByEmailCompiled(_context, email);
    
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        var users = new List<User>();
        await foreach(var user in GetActiveUsersCompiled(_context))
        {
            users.Add(user);
        }
        return users;
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(string role)
    {
        var users = new List<User>();
        await foreach (var user in GetByRoleCompiled(_context, role))
            users.Add(user);
        return users;
    }
    
    public async Task<bool> IsEmailUniqueAsync(string email)
        => !await _dbSet.AnyAsync(u => u.Email.ToLower() == email.ToLower());


    // Eager Loading
    // Eager Loading = load related data IN THE SAME SQL query using JOIN
    // Everything is loaded upfront — no extra queries later

    // Loads User + their UserProfile in ONE query
    // SQL: SELECT * FROM Users u LEFT JOIN UserProfiles up ON up.UserId = u.Id
    public async Task<User?> GetWithProfileAsync(int id)
    {
        return await GetByIdWithIncludesAsync(
            id,
            splitQuery: true,
            q => q.Include(u => u.Profile)
        );
    }

    public async Task<User?> GetWithTeamsAsync(int id)
    {
        return await GetByIdWithIncludesAsync(
            id,
            splitQuery: true,
            q => q.Include(u => u.TeamMembers)
                    .ThenInclude(tm => tm.Team)
        );
    }

    public async Task<User?> GetWithAllRelatedAsync(int id)
    {
        return await GetByIdWithIncludesAsync(
            id,
            splitQuery: true,
            q => q.Include(u => u.Profile),
            q => q.Include(u => u.TeamMembers)
                    .ThenInclude(tm => tm.Team),
            q => q.Include(u => u.OwnedProjects),
            q => q.Include(u => u.AssignedTasks)
        );
    }

    // ── EXPLICIT LOADING ───────────────────────────────────────────────────
    // Explicit Loading = load related data ON DEMAND with a separate SQL query
    // Useful when you already have the entity and want to load ONE specific relation

    // Loads Profile for an already-loaded User
    // SQL: SELECT * FROM UserProfiles WHERE UserId = @userId
    public async Task LoadProfileAsync(User user)
    {
        await _context.Entry(user)
                .Reference(u => u.Profile) // Reference = single navigation
                .LoadAsync();
    }


    // Loads TeamMembers collection for ans already-loaded User
    // SQL: SELECT * FROM TeamMembers WHERE UserID = @userId
    public async Task LoadTeamsAsync(User user)
        => await _context.Entry(user)
            .Collection(u => u.TeamMembers)     // Collection = many navigation
            .LoadAsync();


    // Returns the current EntityState of a tracked entity
    // States: Added, Modified, Deleted, Unchanged, Detached
    public EntityState GetEntityState(User user)
        => _context.Entry(user).State;

    // Detaches an entity from the DbContext
    // After detach: EF Core no longer tracks it
    // EntityState becomes Detached
    public void DetachEntity(User user)
        => _context.Entry(user).State = EntityState.Detached;


    // ── FromSqlRaw ────────────────────────────────────────────────────────────
    // Executes raw SQL and maps results to User entities
    // EF Core TRACKS these entities (change tracking is ON)
    // Use when you need full entity tracking after the query
    public async Task<IEnumerable<User>> GetByRoleRawSqlAsync(string role)
    {
        return await _dbSet
            .FromSqlRaw("SELECT * FROM Users WHERE Role = {0}", role)
            // {0} = parameterized — safe from SQL injection
            // EF Core replaces {0} with a proper SQL parameter
            .ToListAsync();
    
    }

    // FromSqlRaw with named SQL parameter
    public async Task<User?> GetByEmailRawSqlAsync(string email)
        => await _dbSet
            .FromSqlRaw(
                "SELECT * FROM Users WHERE Email = {0}",
                email)
            .FirstOrDefaultAsync();
    

    // ── ExecuteSqlRaw ──────────────────────────────────────────────────────────
    // Executes raw SQL for commands that DON'T return entities
    // Returns: number of rows affected
    // Use for UPDATE, DELETE, INSERT when you don't need tracked entities back
    public async Task<int> DeactivateUserRawSqlAsync(int userId)
    {
        return await _context.Database
            .ExecuteSqlRawAsync(
                "UPDATE Users SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = {0}",
                userId
            );
            // Returns: 1 if row was updated, 0 if not found
    }

    // Bulk update — deactivate all users with a specific role
    public async Task<int> BulkDeactivateByRoleAsync(string role)
        => await _context.Database
            .ExecuteSqlRawAsync(
                "UPDATE Users SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Role = {0}",
                role);
            // Returns: number of rows affected

    
    // ── Stored Procedures ──────────────────────────────────────────────────────
    // Call a stored procedure that RETURNS rows → use FromSqlRaw
    public async Task<IEnumerable<User>> GetActiveUsersByRoleSpAsync(string role)
        => await _dbSet
            .FromSqlRaw("EXEC sp_GetActiveUsersByRole {0}", role)
            .ToListAsync();
        
    
    // Call a stored procedure that does NOT return rows → use ExecuteSqlRaw
    public async Task<int> UpdateUserRoleSpAsync(int userId, string newRole)
        => await _context.Database
            .ExecuteSqlRawAsync(
                "EXEC sp_UpdateUserRole {0}, {1}",
                userId, newRole
            );


    // ── SOFT DELETE ────────────────────────────────────────────────────────────
    // Instead of DELETE we set IsDeleted = true
    // Global Query Filter automatically hides these from all future queries
    public async Task SoftDeleteAsync(int id)
    {
        var user = await _dbSet.FindAsync(id);
        if (user is null) return;

        user.IsDeleted = true;      // ← mark as deleted — NOT removed from DB
        _dbSet.Update(user);
        // Global filter: WHERE IsDeleted = 0
        // After this: user won't appear in ANY query unless filter is ignored
    }

    // ── SHADOW PROPERTIES ──────────────────────────────────────────────────────
    // Shadow properties are accessed via EF.Property<T>() — not C# properties
    public async Task<string?> GetCreatedByAsync(int id)
    {
        var user = await _dbSet.FindAsync(id);
        if (user is null) return null;

        // Read shadow property value from EF Core's change tracker
        return _context.Entry(user)
            .Property<string>("CreatedBy")
            .CurrentValue;
    }

    public async Task SetLastLoginAsync(int id)
    {
        var user = await _dbSet.FindAsync(id);
        if (user is null) return;

        // Set shadow property value — no C# property needed on User!
        _context.Entry(user)
            .Property<DateTime?>("LastLoginAt")
            .CurrentValue = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    // ── CONCURRENCY ────────────────────────────────────────────────────────────
    public async Task<User?> GetWithRowVersionAsync(int id)
        => await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

    // ── IGNORE GLOBAL FILTER ───────────────────────────────────────────────────
    // IgnoreQueryFilters() bypasses ALL global filters on this query
    // Returns soft-deleted users too
    public async Task<IEnumerable<User>> GetAllIncludingDeletedAsync()
        => await _dbSet
            .IgnoreQueryFilters()       // ← bypasses WHERE IsDeleted = 0
            .ToListAsync();
}



