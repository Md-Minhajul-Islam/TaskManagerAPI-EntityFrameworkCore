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
            .HasDefaultValue("Member");     // DB default value

        // ── IsActive ───────────────────────────────────────────────
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasColumnName("IsActive")
            .HasDefaultValue(true);

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
        // builder.HasIndex(u => u.Email)
        //     .IsUnique()
        //     .HasDatabaseName("IX_Users_Email");

        // ── Index on IsActive (for filtering active users) ─────────
        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");
    

        // ── Alternate Key ──────────────────────────────────────────────────────────
        // An Alternate Key is different from a Unique Index:
        //   - Unique Index   → just prevents duplicates
        //   - Alternate Key  → prevents duplicates AND can be used as a FK target
        //                      from another table (it's a real key constraint)
        builder.HasAlternateKey(u => u.Email)
            .HasName("AK_Users_Email");
    
    }
}