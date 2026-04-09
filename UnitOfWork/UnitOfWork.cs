using TaskManagerAPI.Data;
using TaskManagerAPI.Repositories.Implementations;
using TaskManagerAPI.Repositories.Interfaces;

namespace TaskManagerAPI.UnitOfWork;

public class UnitOfWorkImp : IUnitOfWork
{
    private readonly AppDbContext _context;

    // Lazy initialization - repository are created only when accessed
    private IUserRepository? _users;

    public UnitOfWorkImp(AppDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);



    // All repositories share the same DbContext
    // so this ONE call saves All pending changes;
    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();
    
    public void Dispose()
        => _context.Dispose();

}