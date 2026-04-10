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