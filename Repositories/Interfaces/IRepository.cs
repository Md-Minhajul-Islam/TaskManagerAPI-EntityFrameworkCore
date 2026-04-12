using System.Linq.Expressions;

namespace TaskManagerAPI.Repositories.Interfaces
{
    // Generic repository defines standard CRUD operations
    // that every entity repository will have
    public interface IRepository<T> where T : class
    {
        // Query
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync();


        // Eager Loading
        Task<T?> GetByIdWithIncludesAsync(int id, params Func<IQueryable<T>, IQueryable<T>>[] includes);

        Task<IEnumerable<T>> GetAllWithIncludesAsync(params Func<IQueryable<T>, IQueryable<T>>[] includes);


        // Commands
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
    }
}
