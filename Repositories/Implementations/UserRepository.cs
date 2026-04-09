using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;
using TaskManagerAPI.Models;
using TaskManagerAPI.Repositories.Interfaces;

namespace TaskManagerAPI.Repositories.Implementations;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
        
    }

    public async Task<User?> GetByEmailAsync(string email)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
        => await _dbSet
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    
    public async Task<bool> IsEmailUniqueAsync(string email)
        => !await _dbSet.AnyAsync(u => u.Email.ToLower() == email.ToLower());
}

