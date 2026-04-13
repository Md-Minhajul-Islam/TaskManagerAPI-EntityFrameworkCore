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
}