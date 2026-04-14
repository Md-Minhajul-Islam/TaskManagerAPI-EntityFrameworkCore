using Microsoft.EntityFrameworkCore.Storage;
using TaskManagerAPI.Repositories.Interfaces;
using TaskManagerAPI.Services.Interfaces;

namespace TaskManagerAPI.UnitOfWork;

// IUnitOfWork gives access to all repositories
// and One SaveChangesAsync for all of them together

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users {get;}
    ITaskRepository Tasks {get;}
    // Single commit point - all repository changes
    // saved in ONE database transaction
    Task<int> SaveChangesAsync();

    // Transactions
    // BeginTransaction - start an explicit DB transaction
    // All operatons after this share the same transaction
    Task<IDbContextTransaction> BeginTransactionAsync();

    // Commit - saves all pending changes and commits the transaction
    Task CommitAsync(IDbContextTransaction transaction);

    // Rollback - undones All changes since BeginTransaction
    Task RollbackAsync(IDbContextTransaction transaction);
}