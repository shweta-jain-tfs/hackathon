using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.LabCommunity.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "LabCommunity");

            migrationBuilder.CreateTable(
                name: "Configurations",
                schema: "LabCommunity",
                columns: table => new
                {
                    LaboratoryCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    CheckInIntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    SystemStatusEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SystemStatusIntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    ReportInstrumentErrorsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ReportCalibrationAndQualityResultsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.LaboratoryCode);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                schema: "LabCommunity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LaboratoryCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    EnrolledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ValidCredentials = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportItems",
                schema: "LabCommunity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExportItemType = table.Column<int>(type: "int", nullable: false),
                    DeduplicationKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayloadVersion = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExportItems_DeduplicationKey",
                schema: "LabCommunity",
                table: "ExportItems",
                column: "DeduplicationKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configurations",
                schema: "LabCommunity");

            migrationBuilder.DropTable(
                name: "Enrollments",
                schema: "LabCommunity");

            migrationBuilder.DropTable(
                name: "ExportItems",
                schema: "LabCommunity");
        }
    }
}
