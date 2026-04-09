using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Data
{
    // Db context is the bridge between your C# code and the database.
    // It manages database connections, tracks changes, and saved data.
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }

        // DbSet = represents a Table in the database
        // EF Core uses this to query and save User records
        public DbSet<User> Users => Set<User>();



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Scans the assembly and automatically finds + applies
            // every class that implements IEntityTypeConfiguration<T>
            // So UserConfiguration.Configure() is called automatically here
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly
            );
        }



        // Override SaveChangesAsync to auto-set UpdatedAt
        // This is Change Tracking in action - EF Core tells us
        // which entities changed before saving
        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default
        )
        {
            foreach(var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if(entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }
            return await base.SaveChangesAsync(cancellationToken);
        }
    } 
} 
