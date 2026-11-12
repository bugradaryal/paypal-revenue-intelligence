using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountApprovalState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPendingApproval",
                schema: "platform",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE platform.users SET \"IsPendingApproval\" = true WHERE \"IsActive\" = false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPendingApproval",
                schema: "platform",
                table: "users");
        }
    }
}
