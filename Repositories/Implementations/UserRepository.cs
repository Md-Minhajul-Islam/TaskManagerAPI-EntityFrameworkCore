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


    // Eager Loading
    // Eager Loading = load related data IN THE SAME SQL query using JOIN
    // Everything is loaded upfront — no extra queries later

    // Loads User + their UserProfile in ONE query
    // SQL: SELECT * FROM Users u LEFT JOIN UserProfiles up ON up.UserId = u.Id
    public async Task<User?> GetWithProfileAsync(int id)
    {
        return await GetByIdWithIncludesAsync(
            id,
            q => q.Include(u => u.Profile)
        );
    }

    public async Task<User?> GetWithTeamsAsync(int id)
    {
        return await GetByIdWithIncludesAsync(
            id,
            q => q.Include(u => u.TeamMembers)
                    .ThenInclude(tm => tm.Team)
        );
    }

    public async Task<User?> GetWithAllRelatedAsync(int id)
    {
        return await GetByIdWithIncludesAsync(
            id,
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



}

