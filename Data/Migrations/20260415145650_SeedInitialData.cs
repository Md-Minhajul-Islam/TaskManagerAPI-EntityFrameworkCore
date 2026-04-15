using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskManagerAPI_EntityFrameworkCore.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Labels",
                columns: new[] { "Id", "Color", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "#FF0000", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bug", null },
                    { 2, "#00FF00", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Feature", null },
                    { 3, "#0000FF", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Improvement", null },
                    { 4, "#FFA500", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Documentation", null },
                    { 5, "#800080", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Testing", null },
                    { 6, "#FF4500", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Critical", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Email", "FullName", "IsActive", "LastLoginAt", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { 10, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "admin@taskmanager.com", "System Admin", true, null, "ADMIN", null },
                    { 11, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "demo@taskmanager.com", "Demo User", true, null, "MEMBER", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Labels",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Labels",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Labels",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Labels",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Labels",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Labels",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 11);
        }
    }
}
