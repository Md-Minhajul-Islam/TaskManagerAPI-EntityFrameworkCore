using TaskManagerAPI.Repositories.Interfaces;

namespace TaskManagerAPI.UnitOfWork;

// IUnitOfWork gives access to all repositories
// and One SaveChangesAsync for all of them together

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users {get;}

    // Single commit point - all repository changes
    // saved in ONE database transaction
    Task<int> SaveChangesAsync();
}