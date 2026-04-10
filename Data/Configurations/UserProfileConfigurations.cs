using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(up => up.Id);

        builder.Property(up => up.Id)
            .UseIdentityColumn();
        
        builder.Property(up => up.Bio)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("Bio");

        builder.Property(up => up.AvatarUrl)
            .IsRequired(false)
            .HasMaxLength(255)
            .HasColumnName("AvatarUrl");
        
        builder.Property(up => up.GitHubUrl)
            .IsRequired(false)
            .HasMaxLength(255)
            .HasColumnName("GitHubUrl");

        // ── Foreign Key ────────────────────────────────────────────
        // UserId is the Foreign Key that points to Users.Id
        // This means every UserProfile row must have a valid UserId
        // that exists in the Users table
        builder.Property(up => up.UserId)
            .IsRequired()
            .HasColumnName("UserId");
        
        // Explicitly configure the Foreign Key constraint
        // We'll configure the full relationship (navigation properties)
        // in Step 05 — here we just set up the FK column
        builder.HasIndex(up => up.UserId)
            .IsUnique()
            .HasDatabaseName("IX_UserProfile_UserId");


    }
}