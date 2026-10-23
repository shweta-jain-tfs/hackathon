using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.TaskCard.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "TaskCard");

            migrationBuilder.CreateTable(
                name: "TaskCards",
                schema: "TaskCard",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InstrumentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CardType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TriggerState",
                schema: "TaskCard",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CardState = table.Column<int>(type: "int", nullable: false),
                    Trigger = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TaskCardId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TriggerState", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TriggerState_TaskCards_TaskCardId",
                        column: x => x.TaskCardId,
                        principalSchema: "TaskCard",
                        principalTable: "TaskCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TriggerState_TaskCardId",
                schema: "TaskCard",
                table: "TriggerState",
                column: "TaskCardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TriggerState",
                schema: "TaskCard");

            migrationBuilder.DropTable(
                name: "TaskCards",
                schema: "TaskCard");
        }
    }
}
