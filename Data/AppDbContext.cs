using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User>        Users        => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Team>        Teams        => Set<Team>();
    public DbSet<TeamMember>  TeamMembers  => Set<TeamMember>();
    public DbSet<Project>     Projects     => Set<Project>();
    public DbSet<Sprint>      Sprints      => Set<Sprint>();
    public DbSet<TaskItem>    Tasks        => Set<TaskItem>();
    public DbSet<Comment>     Comments     => Set<Comment>();
    public DbSet<Label>       Labels       => Set<Label>();
    public DbSet<TaskLabel>   TaskLabels   => Set<TaskLabel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Scans the assembly and automatically finds + applies
        // every class that implements IEntityTypeConfiguration<T>
        // So UserConfiguration.Configure() is called automatically here
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);

        
        // ── Global Query Filters ───────────────────────────────────────────
        // These filters are AUTOMATICALLY applied to EVERY query on these entities
        // You never need to add .Where(u => !u.IsDeleted) manually again
        // EF Core injects it into every SQL query behind the scenes

        // Soft delete filter on User - only return non-deleted users
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => !u.IsDeleted);
        // SQL added automatically: WHERE IsDeleted = 0

        // Soft delete filter on Project
        modelBuilder.Entity<Project>()
            .HasQueryFilter(p => !p.IsDeleted);

        // Soft delete filter on TaskItem
        modelBuilder.Entity<TaskItem>()
            .HasQueryFilter(t => !t.IsDeleted);

        // Soft delete filter on Comment
        modelBuilder.Entity<Comment>()
            .HasQueryFilter(c => !c.IsDeleted);

    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}