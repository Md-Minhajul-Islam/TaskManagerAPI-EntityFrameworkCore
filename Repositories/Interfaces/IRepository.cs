using System.Linq.Expressions;

namespace TaskManagerAPI.Repositories.Interfaces
{
    // Generic repository defines standard CRUD operations
    // that every entity repository will have
    public interface IRepository<T> where T : class
    {
        // Basic Queries
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
    
        // LINQ Querying
        // Where - filter by predicate
        Task<IEnumerable<T>> WhereAsync(
            // The Expression allows Entity Framework Core to translate 
            // the lambda expression into SQL so filtering happens in the database.
            Expression<Func<T, bool>> predicate     
        );

        // Where + OrderBy - filter and sort
        Task<IEnumerable<T>> WhereOrderedAsync(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, object>> orderBy,
            bool descending = false
        );

        // Pagination - skip/take with optional filter
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? prediate = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false
        );
    }
}
