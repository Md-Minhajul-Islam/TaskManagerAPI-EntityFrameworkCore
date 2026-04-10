using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("TaskItems");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Todo");

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Medium");

        builder.Property(t => t.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // One-to-Many: Project -> Tasks
        builder.HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // One-to-Many: User (Reporter) -> Tasks
        builder.HasOne(t => t.Reporter)
            .WithMany(u => u.ReportedTasks)
            .HasForeignKey(t => t.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // One-to-Many: User (Assignee) -> Tasks
        builder.HasOne(t => t.Assignee)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssigneeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
        // SetNull -> deleting a User sets AssigneeId to NULL

        // One-to-Many: Sprint -> Tasks
        builder.HasOne(t => t.Sprint)
            .WithMany(s => s.Tasks)
            .HasForeignKey(t => t.SprintId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction); 

    }
}