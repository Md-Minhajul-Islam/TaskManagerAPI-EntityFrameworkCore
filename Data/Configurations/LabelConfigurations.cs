using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Data.Configurations;

public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.ToTable("Labels");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Color)
            .IsRequired()
            .HasMaxLength(7)
            .HasDefaultValue("#000000");

        builder.Property(l => l.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");


        // ── Seed Data ──────────────────────────────────────────────────────
        // HasData seeds rows into the DB when migration runs
        // Id MUST be hardcoded — EF Core uses it to track changes
        // If you change a seeded row, EF Core generates an UPDATE migration
        // If you remove a seeded row, EF Core generates a DELETE migration
        builder.HasData(
            new Label
            {
                Id        = 1,
                Name      = "Bug",
                Color     = "#FF0000",       // Red
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Label
            {
                Id        = 2,
                Name      = "Feature",
                Color     = "#00FF00",       // Green
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Label
            {
                Id        = 3,
                Name      = "Improvement",
                Color     = "#0000FF",       // Blue
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Label
            {
                Id        = 4,
                Name      = "Documentation",
                Color     = "#FFA500",       // Orange
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Label
            {
                Id        = 5,
                Name      = "Testing",
                Color     = "#800080",       // Purple
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Label
            {
                Id        = 6,
                Name      = "Critical",
                Color     = "#FF4500",       // OrangeRed
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}