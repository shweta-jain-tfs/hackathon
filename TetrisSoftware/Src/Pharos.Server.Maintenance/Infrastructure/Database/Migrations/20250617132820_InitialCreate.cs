using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.Maintenance.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Maintenance");

            migrationBuilder.CreateTable(
                name: "Activities",
                schema: "Maintenance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityType = table.Column<int>(type: "int", nullable: false),
                    ScheduledType = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    LastCompleted = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Next = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InstrumentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaintenanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_MaintenanceId",
                schema: "Maintenance",
                table: "Activities",
                column: "MaintenanceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities",
                schema: "Maintenance");
        }
    }
}
