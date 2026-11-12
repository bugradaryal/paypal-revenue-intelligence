using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceImportSkipped : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InvoiceImportSkipped",
                schema: "revenue",
                table: "sync_runs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceImportSkipped",
                schema: "revenue",
                table: "sync_runs");
        }
    }
}
