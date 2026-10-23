using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.Result.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Result");

            migrationBuilder.CreateTable(
                name: "Blanks",
                schema: "Result",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BlankType = table.Column<int>(type: "int", nullable: false),
                    ResultOk = table.Column<bool>(type: "bit", nullable: false),
                    LowerLimit = table.Column<double>(type: "float", nullable: true),
                    MeanVoltage = table.Column<double>(type: "float", nullable: false),
                    UpperLimit = table.Column<double>(type: "float", nullable: false),
                    DevSolutionBarcode = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    DevSolutionCellPosition = table.Column<int>(type: "int", nullable: false),
                    StopSolutionBarcode = table.Column<string>(type: "nvarchar(43)", maxLength: 43, nullable: false),
                    MeasurementTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blanks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Curves",
                schema: "Result",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    ConjugateLotNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    ImmunoCapLotNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    CalculationType = table.Column<int>(type: "int", nullable: false),
                    CalculationParameterA = table.Column<double>(type: "float", nullable: true),
                    CalculationParameterB = table.Column<double>(type: "float", nullable: true),
                    CalculationParameterC = table.Column<double>(type: "float", nullable: true),
                    CalculationParameterD = table.Column<double>(type: "float", nullable: true),
                    CalculationParameterE = table.Column<double>(type: "float", nullable: true),
                    CurveFittedFlag = table.Column<bool>(type: "bit", nullable: false),
                    IterationsUsed = table.Column<int>(type: "int", nullable: true),
                    Judgment = table.Column<int>(type: "int", nullable: false),
                    MaxIterationsAllowed = table.Column<int>(type: "int", nullable: true),
                    MeasurementDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FirstDayToUse = table.Column<DateOnly>(type: "date", nullable: false),
                    LastDayToUse = table.Column<DateOnly>(type: "date", nullable: false),
                    ZeroPointCalculationNumberOfIntervals = table.Column<int>(type: "int", nullable: true),
                    NumberOfCalibrationPointsInZeroPointCalculation = table.Column<int>(type: "int", nullable: true),
                    ZeroPointCalculationCoefficientAlfa = table.Column<double>(type: "float", nullable: true),
                    ZeroPointCalculationCoefficientBeta = table.Column<double>(type: "float", nullable: true),
                    ErrorPrecisionFlag = table.Column<bool>(type: "bit", nullable: true),
                    ErrorFlatCurveFlag = table.Column<bool>(type: "bit", nullable: true),
                    ErrorLowerPartTooHighFlag = table.Column<bool>(type: "bit", nullable: true),
                    ErrorSingleCalibratorAboveRangeFlag = table.Column<bool>(type: "bit", nullable: true),
                    ErrorSingleCalibratorBelowRangeFlag = table.Column<bool>(type: "bit", nullable: true),
                    ErrorNoRisingTrendFlag = table.Column<bool>(type: "bit", nullable: true),
                    CurveControlDisabledFlag = table.Column<bool>(type: "bit", nullable: true),
                    ZeroPointSwitchConcentration = table.Column<double>(type: "float", nullable: true),
                    CurveMinConcentration = table.Column<double>(type: "float", nullable: true),
                    CurveMaxConcentration = table.Column<double>(type: "float", nullable: true),
                    LastCurveControlsAborted = table.Column<bool>(type: "bit", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curves", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlankReplicates",
                schema: "Result",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BlankId = table.Column<int>(type: "int", nullable: false),
                    MeasurementTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FluorometerValue = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlankReplicates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlankReplicates_Blanks_BlankId",
                        column: x => x.BlankId,
                        principalSchema: "Result",
                        principalTable: "Blanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalyticalRuns",
                schema: "Result",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentAnalyticalRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    StartDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConjugateLotNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    CarrierLotNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InstrumentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurveId = table.Column<int>(type: "int", nullable: true),
                    RunStatus = table.Column<int>(type: "int", nullable: false),
                    CurveSource = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CurveState = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FrozenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CmrRunId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InstrumentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurveControlTotalJudgment = table.Column<int>(type: "int", nullable: false),
                    QualityControlIssue = table.Column<int>(type: "int", nullable: false),
                    CurveControlIssue = table.Column<int>(type: "int", nullable: false),
                    TestIssue = table.Column<int>(type: "int", nullable: false),
                    CalibrationCurveIssue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticalRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticalRuns_Curves_CurveId",
                        column: x => x.CurveId,
                        principalSchema: "Result",
                        principalTable: "Curves",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CurrentCurves",
                schema: "Result",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurveId = table.Column<int>(type: "int", nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    ConjugateLotNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    ImmunoCapLotNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentCurves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrentCurves_Curves_CurveId",
                        column: x => x.CurveId,
                        principalSchema: "Result",
                        principalTable: "Curves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlankReplicateErrors",
                schema: "Result",
                columns: table => new
                {
                    BlankReplicateId = table.Column<int>(type: "int", nullable: false),
                    ErrorCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlankReplicateErrors", x => new { x.BlankReplicateId, x.ErrorCode })
                        .Annotation("SqlServer:Clustered", true);
                    table.ForeignKey(
                        name: "FK_BlankReplicateErrors_BlankReplicates_BlankReplicateId",
                        column: x => x.BlankReplicateId,
                        principalSchema: "Result",
                        principalTable: "BlankReplicates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                schema: "Result",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnalyticalRunId = table.Column<int>(type: "int", nullable: true),
                    ResultType = table.Column<int>(type: "int", nullable: false),
                    InstrumentNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    InstrumentDilutionFactor = table.Column<int>(type: "int", nullable: false),
                    PreDilutionFactor = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MeanResponse = table.Column<double>(type: "float", nullable: true),
                    MeanResponseAsText = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    MeanConcentration = table.Column<double>(type: "float", nullable: true),
                    MeanConcentrationAsText = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MeanConcentrationStatus = table.Column<int>(type: "int", nullable: false),
                    NumberOfReplicates = table.Column<int>(type: "int", nullable: false),
                    PercentCvResponse = table.Column<double>(type: "float", nullable: true),
                    PercentCvConcentration = table.Column<double>(type: "float", nullable: true),
                    MeasurementDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OperatorComment = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CurveId = table.Column<int>(type: "int", nullable: true),
                    CalibratorPointName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CalibratorTestResult_ExpectedConcentration = table.Column<double>(type: "float", nullable: true),
                    NormDoseCheckSummary = table.Column<int>(type: "int", nullable: true),
                    ArtificialResponse = table.Column<double>(type: "float", nullable: true),
                    LevelIndex = table.Column<int>(type: "int", nullable: true),
                    TestOrderGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GroupSize = table.Column<int>(type: "int", nullable: true),
                    CurveControlPointName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CurveControlTestResult_ExpectedConcentration = table.Column<double>(type: "float", nullable: true),
                    JudgmentSummary = table.Column<int>(type: "int", nullable: true),
                    QualityControlName = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    QualityControlType = table.Column<int>(type: "int", nullable: true),
                    ExpectedConcentration = table.Column<double>(type: "float", nullable: true),
                    ExpectedStdDev = table.Column<double>(type: "float", nullable: true),
                    StdDevLimit = table.Column<double>(type: "float", nullable: true),
                    MinValue = table.Column<double>(type: "float", nullable: true),
                    ExcludeMinValue = table.Column<bool>(type: "bit", nullable: true),
                    MaxValue = table.Column<double>(type: "float", nullable: true),
                    ExcludeMaxValue = table.Column<bool>(type: "bit", nullable: true),
                    Judgment = table.Column<int>(type: "int", nullable: true),
                    QualityControlTestResult_MeanClass = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    QualityControlTestResult_MeanCutOff = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    QualityControlTestResult_MeanQuotient = table.Column<double>(type: "float", nullable: true),
                    QualityControlTestResult_MeanQuotientAsText = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    QualityControlTestResult_LowestClassFlag = table.Column<bool>(type: "bit", nullable: true),
                    QualityControlTestResult_HighestClassFlag = table.Column<bool>(type: "bit", nullable: true),
                    TubeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RequestUid_SampleId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RequestUid_RequestCreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MeanClass = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    MeanCutOff = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    MeanQuotient = table.Column<double>(type: "float", nullable: true),
                    MeanQuotientAsText = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    LowestClassFlag = table.Column<bool>(type: "bit", nullable: true),
                    HighestClassFlag = table.Column<bool>(type: "bit", nullable: true),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: true),
                    IsReportedToLis = table.Column<bool>(type: "bit", nullable: true),
                    SampleIdNaturalSortKey = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true, computedColumnSql: "dbo.NaturalSortKey(RequestUid_SampleId)"),
                    TestNameNaturalSortKey = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true, computedColumnSql: "dbo.NaturalSortKey(TestName)"),
                    MeanClassNaturalSortKey = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true, computedColumnSql: "dbo.NaturalSortKey(MeanClass)"),
                    MeanCutOffNaturalSortKey = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true, computedColumnSql: "dbo.NaturalSortKey(MeanCutOff)"),
                    MeanQuotientNaturalSortKey = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true, computedColumnSql: "dbo.NaturalSortKey(MeanQuotientAsText)"),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestResults_AnalyticalRuns_AnalyticalRunId",
                        column: x => x.AnalyticalRunId,
                        principalSchema: "Result",
                        principalTable: "AnalyticalRuns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestResults_Curves_CurveId",
                        column: x => x.CurveId,
                        principalSchema: "Result",
                        principalTable: "Curves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestReplicates",
                schema: "Result",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentReplicateId = table.Column<string>(type: "nvarchar(36)", nullable: false),
                    ReplicateNumber = table.Column<int>(type: "int", nullable: false),
                    TestResultId = table.Column<int>(type: "int", nullable: false),
                    Response = table.Column<double>(type: "float", nullable: true),
                    ResponseAsText = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    ResponseFromInstrument = table.Column<double>(type: "float", nullable: true),
                    Concentration = table.Column<double>(type: "float", nullable: true),
                    ConcentrationAsText = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ConcentrationStatus = table.Column<int>(type: "int", nullable: false),
                    MeasurementDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CmrTestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IrwRow = table.Column<int>(type: "int", nullable: false),
                    IrwSpoke = table.Column<int>(type: "int", nullable: false),
                    ErwRow = table.Column<int>(type: "int", nullable: false),
                    ErwSpoke = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    IsAborted = table.Column<bool>(type: "bit", nullable: false),
                    FluorometerValue = table.Column<double>(type: "float", nullable: true),
                    IsLowResponse = table.Column<bool>(type: "bit", nullable: false),
                    CarrierBarcode = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    UsedBlankId = table.Column<int>(type: "int", nullable: true),
                    ResultType = table.Column<int>(type: "int", nullable: false),
                    NormDoseCheckResult = table.Column<int>(type: "int", nullable: true),
                    CalibratorOriginalResponse = table.Column<double>(type: "float", nullable: true),
                    EditedResponse = table.Column<double>(type: "float", nullable: true),
                    ResponseEditedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponseEditedDateAndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CalibratorBarcode = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    Judgment = table.Column<int>(type: "int", nullable: true),
                    CurveControlBarcode = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    QualityControlBarcode = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    QualityControlTestReplicate_Class = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    QualityControlTestReplicate_LowestClassFlag = table.Column<bool>(type: "bit", nullable: true),
                    QualityControlTestReplicate_HighestClassFlag = table.Column<bool>(type: "bit", nullable: true),
                    QualityControlTestReplicate_CutOff = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    QualityControlTestReplicate_Quotient = table.Column<double>(type: "float", nullable: true),
                    QualityControlTestReplicate_QuotientAsText = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Class = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    LowestClassFlag = table.Column<bool>(type: "bit", nullable: true),
                    HighestClassFlag = table.Column<bool>(type: "bit", nullable: true),
                    CutOff = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Quotient = table.Column<double>(type: "float", nullable: true),
                    QuotientAsText = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestReplicates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestReplicates_TestResults_TestResultId",
                        column: x => x.TestResultId,
                        principalSchema: "Result",
                        principalTable: "TestResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestReplicateErrors",
                schema: "Result",
                columns: table => new
                {
                    TestReplicateId = table.Column<int>(type: "int", nullable: false),
                    ErrorCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestReplicateErrors", x => new { x.TestReplicateId, x.ErrorCode });
                    table.ForeignKey(
                        name: "FK_TestReplicateErrors_TestReplicates_TestReplicateId",
                        column: x => x.TestReplicateId,
                        principalSchema: "Result",
                        principalTable: "TestReplicates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestReplicateReagents",
                schema: "Result",
                columns: table => new
                {
                    TestReplicateId = table.Column<int>(type: "int", nullable: false),
                    ReagentType = table.Column<int>(type: "int", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    CellPosition = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Expired = table.Column<bool>(type: "bit", nullable: false),
                    Udi = table.Column<string>(type: "nvarchar(43)", maxLength: 43, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestReplicateReagents", x => new { x.TestReplicateId, x.Barcode, x.SerialNumber, x.CellPosition, x.LotNumber, x.Expired, x.ReagentType })
                        .Annotation("SqlServer:Clustered", true);
                    table.ForeignKey(
                        name: "FK_TestReplicateReagents_TestReplicates_TestReplicateId",
                        column: x => x.TestReplicateId,
                        principalSchema: "Result",
                        principalTable: "TestReplicates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRuns_CurveId",
                schema: "Result",
                table: "AnalyticalRuns",
                column: "CurveId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticalRuns_InstrumentNumber_InstrumentAnalyticalRunId",
                schema: "Result",
                table: "AnalyticalRuns",
                columns: new[] { "InstrumentNumber", "InstrumentAnalyticalRunId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlankReplicates_BlankId",
                schema: "Result",
                table: "BlankReplicates",
                column: "BlankId");

            migrationBuilder.CreateIndex(
                name: "IX_BlankReplicates_MeasurementTime",
                schema: "Result",
                table: "BlankReplicates",
                column: "MeasurementTime");

            migrationBuilder.CreateIndex(
                name: "IX_Blanks_InstrumentNumber_DevSolutionBarcode_DevSolutionCellPosition",
                schema: "Result",
                table: "Blanks",
                columns: new[] { "InstrumentNumber", "DevSolutionBarcode", "DevSolutionCellPosition" });

            migrationBuilder.CreateIndex(
                name: "IX_CurrentCurves_CurveId",
                schema: "Result",
                table: "CurrentCurves",
                column: "CurveId");

            migrationBuilder.CreateIndex(
                name: "IX_Curves_InstrumentNumber_GroupId",
                schema: "Result",
                table: "Curves",
                columns: new[] { "InstrumentNumber", "GroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestReplicateReagents_Barcode_SerialNumber_CellPosition",
                schema: "Result",
                table: "TestReplicateReagents",
                columns: new[] { "Barcode", "SerialNumber", "CellPosition" });

            migrationBuilder.CreateIndex(
                name: "IX_TestReplicates_InstrumentReplicateId",
                schema: "Result",
                table: "TestReplicates",
                column: "InstrumentReplicateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestReplicates_Response",
                schema: "Result",
                table: "TestReplicates",
                column: "Response");

            migrationBuilder.CreateIndex(
                name: "IX_TestReplicates_TestResultId",
                schema: "Result",
                table: "TestReplicates",
                column: "TestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_AnalyticalRunId_MeasurementDateTime",
                schema: "Result",
                table: "TestResults",
                columns: new[] { "AnalyticalRunId", "MeasurementDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_ApprovalStatus_Created",
                schema: "Result",
                table: "TestResults",
                columns: new[] { "ApprovalStatus", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_CurveId",
                schema: "Result",
                table: "TestResults",
                column: "CurveId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_InstrumentNumber_MethodName_MeasurementDateTime",
                schema: "Result",
                table: "TestResults",
                columns: new[] { "InstrumentNumber", "MethodName", "MeasurementDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_RequestUid_SampleId_RequestUid_RequestCreatedDate",
                schema: "Result",
                table: "TestResults",
                columns: new[] { "RequestUid_SampleId", "RequestUid_RequestCreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_ResultType_ApprovalStatus_Created",
                schema: "Result",
                table: "TestResults",
                columns: new[] { "ResultType", "ApprovalStatus", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestOrderGroupId",
                schema: "Result",
                table: "TestResults",
                column: "TestOrderGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlankReplicateErrors",
                schema: "Result");

            migrationBuilder.DropTable(
                name: "CurrentCurves",
                schema: "Result");

            migrationBuilder.DropTable(
                name: "TestReplicateErrors",
                schema: "Result");

            migrationBuilder.DropTable(
                name: "TestReplicateReagents",
                schema: "Result");

            migrationBuilder.DropTable(
                name: "BlankReplicates",
                schema: "Result");

            migrationBuilder.DropTable(
                name: "TestReplicates",
                schema: "Result");

            migrationBuilder.DropTable(
                name: "Blanks",
                schema: "Result");

            migrationBuilder.DropTable(
                name: "TestResults",
                schema: "Result");

            migrationBuilder.DropTable(
                name: "AnalyticalRuns",
                schema: "Result");

            migrationBuilder.DropTable(
                name: "Curves",
                schema: "Result");
        }
    }
}
