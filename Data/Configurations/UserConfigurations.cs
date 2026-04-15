using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Data.Configurations;

// IEntityTypeConfiguration<User> → interface from EF Core
// Forces you to implement Configure() method
// Keeps ALL Fluent API config for User in one place
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    // This method is called automatically by EF Core
    // when ApplyConfigurationsFromAssembly() runs
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // ── Table ──────────────────────────────────────────────────
        builder.ToTable("Users");

        // ── Primary Key ────────────────────────────────────────────
        // Already set via [Key] annotation but Fluent API is explicit
        builder.HasKey(u => u.Id);

        // ── Id column ─────────────────────────────────────────────
        builder.Property(u => u.Id)
            .UseIdentityColumn()            // Auto-increment (1, 1)
            .HasColumnName("Id");

        // ── FullName ───────────────────────────────────────────────
        builder.Property(u => u.FullName)
            .IsRequired()                   // NOT NULL
            .HasMaxLength(100)              // NVARCHAR(100)
            .HasColumnName("FullName")
            .HasColumnOrder(1);             // Column order in table

        // ── Email ──────────────────────────────────────────────────
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnName("Email")
            .HasColumnOrder(2);

        // ── Role ───────────────────────────────────────────────────
        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("Role")
            .HasDefaultValue("Member")    // DB default value
            .HasConversion(
                v => v.ToUpper(),
                v => v
            );
        // ── Value Converter ────────────────────────────────────────────────────────
        // Store Role as uppercase in DB but use normal casing in C#
        // C#: "admin" or "Admin" → DB: "ADMIN"
        // DB: "ADMIN" → C#: "ADMIN" (reads back as stored)


        // ── Concurrency Token ──────────────────────────────────────────────────────
        // RowVersion is auto-managed by SQL Server
        // EF Core includes it in every UPDATE WHERE clause
        // If the value has changed since we loaded the entity → DbUpdateConcurrencyException
        builder.Property(u => u.RowVersion)
            .IsRowVersion()
            .HasColumnName("RowVersion")
            .IsRequired();

        // ── IsActive ───────────────────────────────────────────────
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasColumnName("IsActive")
            .HasDefaultValue(true);
        
        // IsDeleted
        builder.Property(u => u.IsDeleted)
            .IsRequired()
            .HasColumnName("IsDeleted")
            .HasDefaultValue(false);

        // ── CreatedAt ──────────────────────────────────────────────
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("GETUTCDATE()"); // DB generates timestamp

        // ── UpdatedAt ──────────────────────────────────────────────
        builder.Property(u => u.UpdatedAt)
            .IsRequired(false)              // NULL allowed
            .HasColumnName("UpdatedAt");

        //   ── Unique Index on Email ──────────────────────────────────
        
        // single index - speeds up filtering by role
        builder.HasIndex(u => u.Role)
            .HasDatabaseName("IX_Users_Role");

        
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        // ── Index on IsActive (for filtering active users) ─────────
        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        
        // Composite index - sppeds up queries filltering by BOTH IsActive and Role
        // e.g., "Get all active admins"
        // SQL: WHERE IsActive = 1 AND Role = 'Admin'
        builder.HasIndex(u => new {u.IsActive, u.Role})
            .HasDatabaseName("IX_Users_IsActive_Role");
    
        // Filtered index - only indexes active users
        // Smaller index = faster loopkups when filtering 
        builder.HasIndex(u => u.IsActive)
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("IX_Users_IsActive_Filtered");



        // ── Alternate Key ──────────────────────────────────────────────────────────
        // An Alternate Key is different from a Unique Index:
        //   - Unique Index   → just prevents duplicates
        //   - Alternate Key  → prevents duplicates AND can be used as a FK target
        //                      from another table (it's a real key constraint)
        builder.HasAlternateKey(u => u.Email)
            .HasName("AK_Users_Email");


        
        // ── Shadow Properties ──────────────────────────────────────────────────────
        // These columns exist in the DB but have NO property on the User class
        // EF Core manages them internally — accessed via EF.Property<T>()

        // Shadow property: LastLoginAt — tracks when user last logged in
        // No C# property needed on User class — DB concern only
        builder.Property<DateTime?>("LastLoginAt")
            .HasColumnName("LastLoginAt")
            .IsRequired(false);
        
        // Shadow property: CreatedBy — tracks who created the record
        builder.Property<string>("CreatedBy")
            .HasColumnName("CreatedBy")
            .HasMaxLength(100)
            .IsRequired(false);


        // ── Seed Data ──────────────────────────────────────────────────────────────
    // Seed a default system admin user
    // Hardcoded Id and CreatedAt — required by HasData
    // RowVersion cannot be seeded (auto-managed by SQL Server)
    // Shadow properties (CreatedBy, LastLoginAt) cannot be seeded via HasData
    builder.HasData(
        new User
        {
            Id        = 10,
            FullName  = "System Admin",
            Email     = "admin@taskmanager.com",
            Role      = "ADMIN",            // Value converter stores uppercase
            IsActive  = true,
            IsDeleted = false,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new User
        {
            Id        = 11,
            FullName  = "Demo User",
            Email     = "demo@taskmanager.com",
            Role      = "MEMBER",
            IsActive  = true,
            IsDeleted = false,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        }
    );
    }
}