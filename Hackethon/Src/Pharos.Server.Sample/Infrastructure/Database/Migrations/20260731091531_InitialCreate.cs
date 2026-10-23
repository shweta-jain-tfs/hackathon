using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.Sample.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Sample");

            migrationBuilder.CreateTable(
                name: "SampleTrays",
                schema: "Sample",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    InstrumentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Drawer = table.Column<int>(type: "int", nullable: true),
                    TrayPosition = table.Column<int>(type: "int", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleTrays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SampleTubes",
                schema: "Sample",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TubeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    InstrumentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LoadTimestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SampleId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SampleIdSuffix = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IdentitySource = table.Column<int>(type: "int", nullable: false),
                    RequestCreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HighPriority = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UnloadRequested = table.Column<bool>(type: "bit", nullable: false),
                    InstrumentError = table.Column<int>(type: "int", nullable: false),
                    InternalError = table.Column<int>(type: "int", nullable: false),
                    SampleVolume = table.Column<double>(type: "float", nullable: false),
                    ImageIdentity = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TrayId = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    TubePosition = table.Column<int>(type: "int", nullable: true),
                    TubeBarcode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SampleType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleTubes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SampleTubes_SampleTrays_TrayId",
                        column: x => x.TrayId,
                        principalSchema: "Sample",
                        principalTable: "SampleTrays",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SampleTubes_SampleId_RequestCreatedDate",
                schema: "Sample",
                table: "SampleTubes",
                columns: new[] { "SampleId", "RequestCreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SampleTubes_TrayId",
                schema: "Sample",
                table: "SampleTubes",
                column: "TrayId");

            migrationBuilder.CreateIndex(
                name: "IX_SampleTubes_TubeId",
                schema: "Sample",
                table: "SampleTubes",
                column: "TubeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SampleTubes",
                schema: "Sample");

            migrationBuilder.DropTable(
                name: "SampleTrays",
                schema: "Sample");
        }
    }
}
