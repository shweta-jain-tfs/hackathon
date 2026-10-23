using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.Settings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ProtectedValueColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProtectedValue",
                schema: "Settings",
                table: "Settings",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProtectedValue",
                schema: "Settings",
                table: "Settings");
        }
    }
}
