using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Repositories.Interfaces;

// User-specific queries that don't belong in the generic repository
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<bool> IsEmailUniqueAsync(string email);

    // Eager Loading
    Task<User?> GetWithProfileAsync(int id);
    Task<User?> GetWithTeamsAsync(int id);
    Task<User?> GetWithAllRelatedAsync(int id);

    // Explicit Loading
    Task LoadProfileAsync(User user);
    Task LoadTeamsAsync(User user);

    // ── Tracking / EntityState ─────────────────────────────────
    EntityState GetEntityState(User user);
    void DetachEntity(User user);


    // ── Raw SQL ────────────────────────────────────────────────
    // FromSqlRaw — executes raw SQL, returns tracked entities
    Task<IEnumerable<User>> GetByRoleRawSqlAsync(string role);

    // FromSqlRaw with parameters — safe from SQL injection
    Task<User?>             GetByEmailRawSqlAsync(string email);

    // ExecuteSqlRaw — for UPDATE/DELETE that don't return entities
    Task<int>               DeactivateUserRawSqlAsync(int userId);
    Task<int>               BulkDeactivateByRoleAsync(string role);

    // Stored Procedure — call via FromSqlRaw
    Task<IEnumerable<User>> GetActiveUsersByRoleSpAsync(string role);

    // Stored Procedure — call via ExecuteSqlRaw
    Task<int>               UpdateUserRoleSpAsync(int userId, string newRole);

    // ── Soft Delete ────────────────────────────────────────────
    Task SoftDeleteAsync(int id);

    // ── Shadow Properties ──────────────────────────────────────
    Task<string?> GetCreatedByAsync(int id);
    Task          SetLastLoginAsync(int id);

    // ── Concurrency ────────────────────────────────────────────
    Task<User?> GetWithRowVersionAsync(int id);

    // ── Ignore Global Filter ───────────────────────────────────
    // Get ALL users including soft-deleted ones
    Task<IEnumerable<User>> GetAllIncludingDeletedAsync();


}