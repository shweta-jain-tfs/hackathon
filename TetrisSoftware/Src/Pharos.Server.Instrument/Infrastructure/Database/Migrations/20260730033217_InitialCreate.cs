using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.Instrument.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Instrument");

            migrationBuilder.CreateTable(
                name: "Instruments",
                schema: "Instrument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    InstrumentType = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InstrumentSoftwareVersion = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instruments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssaySchedulers",
                schema: "Instrument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PausedUntilDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssaySchedulers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssaySchedulers_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalSchema: "Instrument",
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InstrumentMethods",
                schema: "Instrument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentId = table.Column<int>(type: "int", nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    WhenToRun = table.Column<int>(type: "int", nullable: false),
                    Calibrator = table.Column<bool>(type: "bit", nullable: false),
                    ForceCalibrator = table.Column<bool>(type: "bit", nullable: false),
                    ConjugateLotNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    CarrierLotNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstrumentMethods_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalSchema: "Instrument",
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InstrumentTechnologies",
                schema: "Instrument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentId = table.Column<int>(type: "int", nullable: false),
                    Technology = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentTechnologies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstrumentTechnologies_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalSchema: "Instrument",
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssaySchedulerDays",
                schema: "Instrument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchedulerId = table.Column<int>(type: "int", nullable: false),
                    Day = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssaySchedulerDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssaySchedulerDays_AssaySchedulers_SchedulerId",
                        column: x => x.SchedulerId,
                        principalSchema: "Instrument",
                        principalTable: "AssaySchedulers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssaySchedulerMethods",
                schema: "Instrument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchedulerDayId = table.Column<int>(type: "int", nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    WhenToRun = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssaySchedulerMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssaySchedulerMethods_AssaySchedulerDays_SchedulerDayId",
                        column: x => x.SchedulerDayId,
                        principalSchema: "Instrument",
                        principalTable: "AssaySchedulerDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssaySchedulerDays_SchedulerId",
                schema: "Instrument",
                table: "AssaySchedulerDays",
                column: "SchedulerId");

            migrationBuilder.CreateIndex(
                name: "IX_AssaySchedulerMethods_SchedulerDayId",
                schema: "Instrument",
                table: "AssaySchedulerMethods",
                column: "SchedulerDayId");

            migrationBuilder.CreateIndex(
                name: "IX_AssaySchedulers_InstrumentId",
                schema: "Instrument",
                table: "AssaySchedulers",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentMethods_InstrumentId",
                schema: "Instrument",
                table: "InstrumentMethods",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentTechnologies_InstrumentId_Technology",
                schema: "Instrument",
                table: "InstrumentTechnologies",
                columns: new[] { "InstrumentId", "Technology" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_SerialNumber",
                schema: "Instrument",
                table: "Instruments",
                column: "SerialNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssaySchedulerMethods",
                schema: "Instrument");

            migrationBuilder.DropTable(
                name: "InstrumentMethods",
                schema: "Instrument");

            migrationBuilder.DropTable(
                name: "InstrumentTechnologies",
                schema: "Instrument");

            migrationBuilder.DropTable(
                name: "AssaySchedulerDays",
                schema: "Instrument");

            migrationBuilder.DropTable(
                name: "AssaySchedulers",
                schema: "Instrument");

            migrationBuilder.DropTable(
                name: "Instruments",
                schema: "Instrument");
        }
    }
}
