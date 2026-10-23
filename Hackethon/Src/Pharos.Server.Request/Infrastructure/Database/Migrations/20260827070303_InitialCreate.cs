using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.Request.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Request");

            migrationBuilder.CreateTable(
                name: "Patients",
                schema: "Request",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PatientNumber = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    MedicalRecordNumber = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    PatientId = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    Diagnosis = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Requestors",
                schema: "Request",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestorId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    OrderingPhysicianCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReflexEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ShortcutCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LatestSampleDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NumberOfReportedSamples = table.Column<int>(type: "int", nullable: false),
                    NumberOfReportedTests = table.Column<int>(type: "int", nullable: false),
                    CounterResetDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requestors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Requests",
                schema: "Request",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SampleId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: true),
                    SampleTubeType = table.Column<int>(type: "int", nullable: true),
                    PreDilutionFactor = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RequestDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HighPriority = table.Column<bool>(type: "bit", nullable: false),
                    RequestorId = table.Column<int>(type: "int", nullable: true),
                    PatientAge = table.Column<int>(type: "int", nullable: true),
                    SampleCollectionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ErrorFlag = table.Column<bool>(type: "bit", nullable: false),
                    RequestState = table.Column<int>(type: "int", nullable: false),
                    SampleVolumeCheck = table.Column<int>(type: "int", nullable: false),
                    IsCountedInRequestor = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Requests_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "Request",
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Requests_Requestors_RequestorId",
                        column: x => x.RequestorId,
                        principalSchema: "Request",
                        principalTable: "Requestors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Issues",
                schema: "Request",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueType = table.Column<int>(type: "int", nullable: false),
                    Urgency = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Issues_Requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "Request",
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestOrders",
                schema: "Request",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    NumberOfReplicates = table.Column<int>(type: "int", nullable: false),
                    InstrumentDilutionFactor = table.Column<int>(type: "int", nullable: false),
                    InstrumentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    LabTestName = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TestSortOrder = table.Column<int>(type: "int", nullable: false),
                    LisChannelId = table.Column<int>(type: "int", nullable: true),
                    TestPanelIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TubeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsReflexTest = table.Column<bool>(type: "bit", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestOrders", x => x.Id);
                    table.UniqueConstraint("AK_TestOrders_Uid", x => x.Uid);
                    table.ForeignKey(
                        name: "FK_TestOrders_Requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "Request",
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReflexDefinitions",
                schema: "Request",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
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
                    ReflexCondition_RuCompareValue = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReflexDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReflexDefinitions_TestOrders_TestOrderId",
                        column: x => x.TestOrderId,
                        principalSchema: "Request",
                        principalTable: "TestOrders",
                        principalColumn: "Uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestOrderCustomParameters",
                schema: "Request",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestOrderId = table.Column<int>(type: "int", nullable: false),
                    CarrierLot = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    DiluentLot = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ConjugateLot = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    DevelopmentLot = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    CustomRunId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomTestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestOrderCustomParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestOrderCustomParameters_TestOrders_TestOrderId",
                        column: x => x.TestOrderId,
                        principalSchema: "Request",
                        principalTable: "TestOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReflexTests",
                schema: "Request",
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
                        principalSchema: "Request",
                        principalTable: "ReflexDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Issues_RequestId",
                schema: "Request",
                table: "Issues",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ReflexDefinitions_TestOrderId",
                schema: "Request",
                table: "ReflexDefinitions",
                column: "TestOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReflexTests_ReflexDefinitionId",
                schema: "Request",
                table: "ReflexTests",
                column: "ReflexDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_PatientId",
                schema: "Request",
                table: "Requests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_RequestId",
                schema: "Request",
                table: "Requests",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_RequestorId",
                schema: "Request",
                table: "Requests",
                column: "RequestorId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_SampleId",
                schema: "Request",
                table: "Requests",
                column: "SampleId");

            migrationBuilder.CreateIndex(
                name: "IX_TestOrderCustomParameters_TestOrderId",
                schema: "Request",
                table: "TestOrderCustomParameters",
                column: "TestOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestOrders_InstrumentNumber",
                schema: "Request",
                table: "TestOrders",
                column: "InstrumentNumber");

            migrationBuilder.CreateIndex(
                name: "IX_TestOrders_RequestId_TestSortOrder",
                schema: "Request",
                table: "TestOrders",
                columns: new[] { "RequestId", "TestSortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TestOrders_State",
                schema: "Request",
                table: "TestOrders",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_TestOrders_Uid",
                schema: "Request",
                table: "TestOrders",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Issues",
                schema: "Request");

            migrationBuilder.DropTable(
                name: "ReflexTests",
                schema: "Request");

            migrationBuilder.DropTable(
                name: "TestOrderCustomParameters",
                schema: "Request");

            migrationBuilder.DropTable(
                name: "ReflexDefinitions",
                schema: "Request");

            migrationBuilder.DropTable(
                name: "TestOrders",
                schema: "Request");

            migrationBuilder.DropTable(
                name: "Requests",
                schema: "Request");

            migrationBuilder.DropTable(
                name: "Patients",
                schema: "Request");

            migrationBuilder.DropTable(
                name: "Requestors",
                schema: "Request");
        }
    }
}
