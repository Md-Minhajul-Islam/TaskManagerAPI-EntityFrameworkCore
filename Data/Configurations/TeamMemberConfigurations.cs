using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Data.Configurations;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("TeamMembers");

                // ── Composite Primary Key ──────────────────────────────────
        // TeamId + UserId TOGETHER form the primary key
        // This means:
        //   - Same TeamId can appear multiple times (multiple users in a team)
        //   - Same UserId can appear multiple times (user in multiple teams)
        //   - But TeamId + UserId combination must be UNIQUE 
        builder.HasKey(tm => new {tm.TeamId, tm.UserId});

        builder.Property(tm => tm.TeamId)
            .IsRequired()
            .HasColumnName("TeamId");

        builder.Property(tm => tm.UserId)
            .IsRequired()
            .HasColumnName("UserId");
        
        builder.Property(tm => tm.Role)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Member");

        builder.Property(tm => tm.JoinedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
    }
}