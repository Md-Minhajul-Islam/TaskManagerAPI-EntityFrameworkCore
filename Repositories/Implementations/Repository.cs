using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;
using TaskManagerAPI.Repositories.Interfaces;

namespace TaskManagerAPI.Repositories.Implementations;

// Generic repository implements all  standard CRUD for any entity
// Specific repository inherit from this and add their own queries
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) 
        => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync()
        => await _dbSet.ToListAsync();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.Where(predicate).ToListAsync();
    
    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.AnyAsync(predicate);
    
    public async Task<int> CountAsync()
        => await _dbSet.CountAsync();

    // ── Eager Loading ──────────────────────────────────────────────────────
    // Applies one or more Include() chains to the query before executing
    // Each include is a function that takes IQueryable<T> and returns IQueryable<T>
    public async Task<T?> GetByIdWithIncludesAsync(
        int id,
        params Func<IQueryable<T>, IQueryable<T>>[] includes
    )
    {
        // Start with the base query
        IQueryable<T> query = _dbSet;

        // Apply each include function in order
        // e.g. q => q.Include(u => u.Profile)
        //      q => q.Include(u => u.TeamMembers).ThenInclude(tm => tm.Team)
        foreach(var include in includes)
        {
            query = include(query);
        }

        // EF Core needs a primary key filter — use FindAsync pattern via Where
        // Note: This works for entities with int Id (our BaseEntity)
        return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
    }
    

    public async Task<IEnumerable<T>> GetAllWithIncludesAsync(
        params Func<IQueryable<T>, IQueryable<T>>[] includes
    )
    {
        IQueryable<T> query = _dbSet;

        foreach(var include in includes)
        {
            query = include(query);
        }
        return await query.ToListAsync();
    }
    public async Task AddAsync(T entity)
        => await _dbSet.AddAsync(entity);
    
    public async Task AddRangeAsync(IEnumerable<T> entities)
        => await _dbSet.AddRangeAsync(entities);
    
    public void Update(T entity)
        => _dbSet.Update(entity);
    
    public void Remove(T entity)
        => _dbSet.Remove(entity);

    public void RemoveRange(IEnumerable<T> entities)
        => _dbSet.RemoveRange(entities);

    // LINQ: Where
    // Filters rows using a predicate expression
    public async Task<IEnumerable<T>> WhereAsync(
        Expression<Func<T, bool>> predicate
    )
    {
        return await _dbSet
                .Where(predicate)
                .ToListAsync();
    }

    // LINQ: Where + OrderBy
    // Filters and Sorts in one query
    public async Task<IEnumerable<T>> WhereOrderedAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, object>> OrderBy,
        bool descending = false
    )
    {
        var query = _dbSet.Where(predicate);
        query = descending ? query.OrderByDescending(OrderBy)
                        : query.OrderBy(OrderBy);

        return await query.ToListAsync();
    }

    // LINQ: Paginaton (Skip / Take)
    // Returns a page of results + total count for pagination metadata
    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool descending = false
    )
    {
        IQueryable<T> query = _dbSet;

        if(predicate is not null)
            query = query.Where(predicate);
        
        // COUNT before pagination - total matching records
        var totalCount = await query.CountAsync();

        if(orderBy is not null)
            query = descending 
                ? query.OrderByDescending(orderBy)
                : query.OrderBy(orderBy);
        
        // Skip / Take - apply pagination
        // Skip = (pageNumber - 1) * pageSize
        var items = await query
            .Skip((pageNumber-1)*pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // ── AsNoTracking ───────────────────────────────────────────────────────────
    // AsNoTracking() tells EF Core:
    //   "Don't watch this entity for changes — I'm only reading it"
    // Benefit: faster queries, less memory — no change tracking snapshot created
    // Use for: GET endpoints, reports, any read-only operation
    public async Task<IEnumerable<T>> GetAllAsNoTrackingAsync()
    => await _dbSet
        .AsNoTracking()         // ← disables change tracking
        .ToListAsync();

    public async Task<T?> GetByIdAsNoTrackingAsync(int id)
        => await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);

    public async Task<IEnumerable<T>> FindAsNoTrackingAsync(
        Expression<Func<T, bool>> predicate)
        => await _dbSet
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync();

    // ── AsNoTrackingWithIdentityResolution ─────────────────────────────────────
    // Middle ground between tracking and no tracking:
    //   - Does NOT track for change detection (fast like AsNoTracking)
    //   - BUT ensures the same entity is only materialized once
    //     (useful when related entities appear multiple times in results)
    // Example: loading Tasks with their Project
    //   Without identity resolution: Project object duplicated for each Task
    //   With identity resolution:    Project object reused across all Tasks
    public async Task<IEnumerable<T>> GetAllAsNoTrackingWithIdentityResolutionAsync()
        => await _dbSet
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync();
}