using Microsoft.EntityFrameworkCore.Storage;
using TaskManagerAPI.Data;
using TaskManagerAPI.Repositories.Implementations;
using TaskManagerAPI.Repositories.Interfaces;

namespace TaskManagerAPI.UnitOfWork;

public class UnitOfWorkImp : IUnitOfWork
{
    private readonly AppDbContext _context;

    // Lazy initialization - repository are created only when accessed
    private IUserRepository? _users;
    private ITaskRepository? _tasks;

    public UnitOfWorkImp(AppDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public ITaskRepository Tasks => _tasks ??= new TaskRepository(_context);


    // All repositories share the same DbContext
    // so this ONE call saves All pending changes;
    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

    
    // Begin Transaction
    // Start an explicit database transaction
    // All SaveChangesAsync() calls after this are part of the SAME transaction
    // Nothing is commited to teh DB until CommitAsync() is called
    public async Task<IDbContextTransaction> BeginTransactionAsync()
        => await _context.Database.BeginTransactionAsync();

    // Commit 
    // Commits all pending changes to the DB permanently
    // After commit - changes are visible to other connections
    public async Task CommitAsync(IDbContextTransaction transaction)
        => await transaction.CommitAsync();
    

    // Rollback
    // Undoes All changes made since BeginTransaction
    // DB returns to exactly the state it was before BeginTransaction
    public async Task RollbackAsync(IDbContextTransaction transaction)
        => await transaction.RollbackAsync();
    
    public void Dispose()
        => _context.Dispose();

}