using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.Settings.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Settings");

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "Settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    LastModified = table.Column<string>(type: "nvarchar(48)", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Settings", x => x.Key));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings",
                schema: "Settings");
        }
    }
}
