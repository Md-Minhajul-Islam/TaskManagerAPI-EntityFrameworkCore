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


        builder.HasOne(up  =>  up.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Explicitly configure the Foreign Key constraint
        // We'll configure the full relationship (navigation properties)
        // in Step 05 — here we just set up the FK column
        builder.HasIndex(up => up.UserId)
            .IsUnique()
            .HasDatabaseName("IX_UserProfile_UserId");



    }
}