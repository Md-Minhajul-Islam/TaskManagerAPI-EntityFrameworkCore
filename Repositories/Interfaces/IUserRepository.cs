using TaskManagerAPI.Models;

namespace TaskManagerAPI.Repositories.Interfaces;

// User-specific queries that don't belong in the generic repository
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<bool> IsEmailUniqueAsync(string email);
}