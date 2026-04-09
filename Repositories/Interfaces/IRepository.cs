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


        // Commands
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
    }
}
