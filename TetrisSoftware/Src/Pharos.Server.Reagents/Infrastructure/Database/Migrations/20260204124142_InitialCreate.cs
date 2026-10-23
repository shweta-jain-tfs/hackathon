using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.Reagents.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Reagents");

            migrationBuilder.CreateTable(
                name: "Lots",
                schema: "Reagents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleType = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OrderType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reagents",
                schema: "Reagents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Barcode = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    Udi = table.Column<string>(type: "nvarchar(43)", maxLength: 43, nullable: true),
                    LotId = table.Column<int>(type: "int", nullable: false),
                    LoadTimestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OverrideExpiration = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reagents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reagents_Lots_LotId",
                        column: x => x.LotId,
                        principalSchema: "Reagents",
                        principalTable: "Lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReagentContents",
                schema: "Reagents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReagentId = table.Column<int>(type: "int", nullable: false),
                    CellPosition = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Unusable = table.Column<bool>(type: "bit", nullable: false),
                    OpenTimestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReagentContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReagentContents_Reagents_ReagentId",
                        column: x => x.ReagentId,
                        principalSchema: "Reagents",
                        principalTable: "Reagents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lots_LotNumber",
                schema: "Reagents",
                table: "Lots",
                column: "LotNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReagentContents_ReagentId",
                schema: "Reagents",
                table: "ReagentContents",
                column: "ReagentId");

            migrationBuilder.CreateIndex(
                name: "IX_Reagents_Barcode",
                schema: "Reagents",
                table: "Reagents",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Reagents_LotId",
                schema: "Reagents",
                table: "Reagents",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_Reagents_Udi",
                schema: "Reagents",
                table: "Reagents",
                column: "Udi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReagentContents",
                schema: "Reagents");

            migrationBuilder.DropTable(
                name: "Reagents",
                schema: "Reagents");

            migrationBuilder.DropTable(
                name: "Lots",
                schema: "Reagents");
        }
    }
}
