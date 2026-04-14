using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagerAPI_EntityFrameworkCore.data.migrations
{
    /// <inheritdoc />
    public partial class AddStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Stored Procedure: Get active users by role ─────────────
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_GetActiveUsersByRole
                    @Role NVARCHAR(20)
                AS
                BEGIN
                    SELECT *
                    FROM Users
                    WHERE Role = @Role
                        AND IsActive = 1
                    ORDER BY FullName;
                END
            ");
            // ── Stored Procedure: Update user role ─────────────────────
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_UpdateUserRole
                    @UserId INT,
                    @NewRole NVARCHAR(20)
                AS
                BEGIN
                    UPDATE Users
                    SET Role = @NewRole,
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = @UserId;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetActiveUsersByRole");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_UpdateUserRole");
        }
    }
}
