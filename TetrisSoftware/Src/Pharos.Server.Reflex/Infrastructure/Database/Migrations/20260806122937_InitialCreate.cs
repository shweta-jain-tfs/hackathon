using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.Reflex.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Reflex");

            migrationBuilder.CreateTable(
                name: "ReflexDefinitions",
                schema: "Reflex",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    Automatic = table.Column<bool>(type: "bit", nullable: false),
                    RequestorId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ReflexCondition_TestName = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ReflexCondition_MethodName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    ReflexCondition_ResultType = table.Column<int>(type: "int", nullable: false),
                    ReflexCondition_CompareType = table.Column<int>(type: "int", nullable: false),
                    ReflexCondition_ConcentrationCompareValue = table.Column<double>(type: "float", nullable: true),
                    ReflexCondition_ConcentrationCompareValueUpperLimit = table.Column<double>(type: "float", nullable: true),
                    ReflexCondition_ConcentrationRawCompareValue = table.Column<double>(type: "float", nullable: true),
                    ReflexCondition_ConcentrationRawCompareValueUpperLimit = table.Column<double>(type: "float", nullable: true),
                    ReflexCondition_ClassCompareValue = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReflexCondition_QuotientCompareValue = table.Column<double>(type: "float", nullable: true),
                    ReflexCondition_QuotientCompareValueUpperLimit = table.Column<double>(type: "float", nullable: true),
                    ReflexCondition_CutOffCompareValue = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReflexCondition_RuCompareValue = table.Column<double>(type: "float", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReflexDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReflexTests",
                schema: "Reflex",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestName = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    NumberOfReplicates = table.Column<int>(type: "int", nullable: false),
                    InstrumentDilutionFactor = table.Column<int>(type: "int", nullable: false),
                    TestSortOrder = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReflexDefinitionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReflexTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReflexTests_ReflexDefinitions_ReflexDefinitionId",
                        column: x => x.ReflexDefinitionId,
                        principalSchema: "Reflex",
                        principalTable: "ReflexDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReflexTests_ReflexDefinitionId",
                schema: "Reflex",
                table: "ReflexTests",
                column: "ReflexDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReflexTests",
                schema: "Reflex");

            migrationBuilder.DropTable(
                name: "ReflexDefinitions",
                schema: "Reflex");
        }
    }
}
