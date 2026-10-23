using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharos.Server.Parameters.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Parameters");

            migrationBuilder.CreateTable(
                name: "Articles",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleNumber = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ArticleType = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(55)", maxLength: 55, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    NumberOfCells = table.Column<int>(type: "int", nullable: true),
                    MaxPiercedHoles = table.Column<int>(type: "int", nullable: true),
                    MethodName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Identity = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    MaxDaysInInstrument = table.Column<int>(type: "int", nullable: true),
                    MaxHoursInInstrument = table.Column<int>(type: "int", nullable: true),
                    Hidden = table.Column<bool>(type: "bit", nullable: false),
                    FriendlyName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Text1 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Text2 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Text3 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Text4 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Shape = table.Column<int>(type: "int", nullable: false),
                    Color1 = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Color2 = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Color3 = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Color4 = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArticlesVersion",
                schema: "Parameters",
                columns: table => new
                {
                    VersionNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticlesVersion", x => x.VersionNumber);
                });

            migrationBuilder.CreateTable(
                name: "IntendedMarkets",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntendedMarkets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Methods",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    MethodNameLis = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DefaultLisChannelId = table.Column<int>(type: "int", nullable: true),
                    FullMethodName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MethodVersion = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    IsRuo = table.Column<bool>(type: "bit", nullable: false),
                    IsSpecializedMethodActivated = table.Column<bool>(type: "bit", nullable: false),
                    IsSpecialMethod = table.Column<bool>(type: "bit", nullable: false),
                    IsTemplate = table.Column<bool>(type: "bit", nullable: false),
                    IsUserDefined = table.Column<bool>(type: "bit", nullable: false),
                    Technology = table.Column<int>(type: "int", nullable: false),
                    PreventDilutionFactorsBelowDefault = table.Column<bool>(type: "bit", nullable: false),
                    CalibratorPiercingOrder = table.Column<int>(type: "int", nullable: false),
                    CalibratorPipettingOrder = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    DefaultConcentrationFactor = table.Column<double>(type: "float", nullable: false),
                    ShowRuPlot = table.Column<bool>(type: "bit", nullable: false),
                    Allow50PercentWash = table.Column<bool>(type: "bit", nullable: false),
                    AllowDilutionInPipette = table.Column<bool>(type: "bit", nullable: false),
                    SubtractBlank = table.Column<bool>(type: "bit", nullable: false),
                    AnalyticalRun_DefaultAllowMonthlyCalibration = table.Column<bool>(type: "bit", nullable: false),
                    AnalyticalRun_UserAllowMonthlyCalibration = table.Column<bool>(type: "bit", nullable: true),
                    AnalyticalRun_DefaultMaxAnalyticalRunSize = table.Column<int>(type: "int", nullable: false),
                    AnalyticalRun_UserMaxAnalyticalRunSize = table.Column<int>(type: "int", nullable: true),
                    AnalyticalRun_RunCalibrationOnCarrierLotChange = table.Column<bool>(type: "bit", nullable: false),
                    Calculation_CalculationType = table.Column<int>(type: "int", nullable: false),
                    Calculation_CalibratorsForLeastSquareCalc = table.Column<int>(type: "int", nullable: false),
                    Calculation_CalibratorStartingPoint = table.Column<int>(type: "int", nullable: false),
                    Calculation_MinCoefficientOfCorrelation = table.Column<double>(type: "float", nullable: false),
                    Calculation_PredictedVarianceA = table.Column<double>(type: "float", nullable: false),
                    Calculation_PredictedVarianceB = table.Column<double>(type: "float", nullable: false),
                    Calculation_RobustWeight = table.Column<int>(type: "int", nullable: false),
                    Calculation_NormalWeight = table.Column<int>(type: "int", nullable: false),
                    Presentation_XAxisScale = table.Column<int>(type: "int", nullable: false),
                    Presentation_XAxisText = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Presentation_XAxisUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Presentation_YAxisScale = table.Column<int>(type: "int", nullable: false),
                    Presentation_YAxisText = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Presentation_YAxisUnit = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Presentation_FixedYAxis = table.Column<bool>(type: "bit", nullable: false),
                    Presentation_FixedYAxisLowerLimit = table.Column<double>(type: "float", nullable: true),
                    Presentation_FixedYAxisUpperLimit = table.Column<double>(type: "float", nullable: true),
                    Reporting_DefaultMinConcentrationToReport = table.Column<double>(type: "float", nullable: false),
                    Reporting_UserMinConcentrationToReport = table.Column<double>(type: "float", nullable: true),
                    Reporting_DefaultTextAboveCutOff = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Reporting_UserTextAboveCutOff = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Reporting_DefaultTextBelowCutOff = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Reporting_UserTextBelowCutOff = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Reporting_DefaultTextQuantitativeAbove = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Reporting_UserTextQuantitativeAbove = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Reporting_DefaultTextQuantitativeBelow = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Reporting_UserTextQuantitativeBelow = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Reporting_DefaultTextQuantitativeDoubtful = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Reporting_UserTextQuantitativeDoubtful = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Reporting_DefaultUseCalculatedTextQuantitativeAbove = table.Column<bool>(type: "bit", maxLength: 7, nullable: false),
                    Reporting_UserUseCalculatedTextQuantitativeAbove = table.Column<bool>(type: "bit", maxLength: 7, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Methods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Panels",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PanelName = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PanelLongName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GroupedPanel = table.Column<bool>(type: "bit", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Panels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParameterUpdatePackageVersion",
                schema: "Parameters",
                columns: table => new
                {
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ImportDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterUpdatePackageVersion", x => x.VersionNumber);
                });

            migrationBuilder.CreateTable(
                name: "QualityControlBarcodeFormats",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    TotalLength = table.Column<int>(type: "int", nullable: false),
                    QcStartPosition = table.Column<int>(type: "int", nullable: false),
                    QcLength = table.Column<int>(type: "int", nullable: false),
                    ContainsLotInformation = table.Column<bool>(type: "bit", nullable: false),
                    LotStartPosition = table.Column<int>(type: "int", nullable: false),
                    LotLength = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityControlBarcodeFormats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CartridgeCellIdentity",
                schema: "Parameters",
                columns: table => new
                {
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    CellPosition = table.Column<int>(type: "int", nullable: false),
                    CellIdentity = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartridgeCellIdentity", x => new { x.ArticleId, x.CellPosition });
                    table.ForeignKey(
                        name: "FK_CartridgeCellIdentity_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "Parameters",
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageBarcode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    OrderingNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    PackageFullName = table.Column<string>(type: "nvarchar(55)", maxLength: 55, nullable: true),
                    IntendedMarketId = table.Column<int>(type: "int", nullable: false),
                    Hidden = table.Column<bool>(type: "bit", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Packages_IntendedMarkets_IntendedMarketId",
                        column: x => x.IntendedMarketId,
                        principalSchema: "Parameters",
                        principalTable: "IntendedMarkets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MethodClassTableBelows",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    ClassTableId = table.Column<int>(type: "int", nullable: false),
                    DefaultTextBelowConcentration = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    UserTextBelowConcentration = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodClassTableBelows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MethodClassTableBelows_Methods_MethodId",
                        column: x => x.MethodId,
                        principalSchema: "Parameters",
                        principalTable: "Methods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MethodClassTables",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    ClassTableId = table.Column<int>(type: "int", nullable: false),
                    ClassTableRowId = table.Column<int>(type: "int", nullable: false),
                    Concentration = table.Column<double>(type: "float", nullable: false),
                    DefaultTextAboveConcentration = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    UserTextAboveConcentration = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodClassTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MethodClassTables_Methods_MethodId",
                        column: x => x.MethodId,
                        principalSchema: "Parameters",
                        principalTable: "Methods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MethodConcentrationUnits",
                schema: "Parameters",
                columns: table => new
                {
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    ConcentrationUnit = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodConcentrationUnits", x => new { x.MethodId, x.UnitId });
                    table.ForeignKey(
                        name: "FK_MethodConcentrationUnits_Methods_MethodId",
                        column: x => x.MethodId,
                        principalSchema: "Parameters",
                        principalTable: "Methods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MethodReagents",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    ReagentType = table.Column<int>(type: "int", nullable: false),
                    DefaultReagentIdentity = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    UserReagentIdentity = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Volume = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodReagents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MethodReagents_Methods_MethodId",
                        column: x => x.MethodId,
                        principalSchema: "Parameters",
                        principalTable: "Methods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QualityControls",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TubeTypeId = table.Column<int>(type: "int", nullable: false),
                    SystemType = table.Column<int>(type: "int", nullable: false),
                    IdOnBarcode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    BarcodeFormatId = table.Column<int>(type: "int", nullable: true),
                    IsQcInTube = table.Column<bool>(type: "bit", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityControls_QualityControlBarcodeFormats_BarcodeFormatId",
                        column: x => x.BarcodeFormatId,
                        principalSchema: "Parameters",
                        principalTable: "QualityControlBarcodeFormats",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PackageItems",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    ArticleAmount = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageItems_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "Parameters",
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageItems_Packages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "Parameters",
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MethodTests",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    FullTestName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    DefaultReplicates = table.Column<int>(type: "int", nullable: false),
                    UserReplicates = table.Column<int>(type: "int", nullable: true),
                    DefaultReportConcentration = table.Column<bool>(type: "bit", nullable: false),
                    UserReportConcentration = table.Column<bool>(type: "bit", nullable: true),
                    DefaultReportClassId = table.Column<int>(type: "int", nullable: false),
                    UserReportClassId = table.Column<int>(type: "int", nullable: true),
                    DefaultReportCutOff = table.Column<bool>(type: "bit", nullable: false),
                    UserReportCutOff = table.Column<bool>(type: "bit", nullable: true),
                    DefaultReportQuotient = table.Column<int>(type: "int", nullable: false),
                    UserReportQuotient = table.Column<int>(type: "int", nullable: true),
                    DefaultMethodConcentrationUnitId = table.Column<int>(type: "int", nullable: false),
                    UserMethodConcentrationUnitId = table.Column<int>(type: "int", nullable: true),
                    HighestConcentrationToReport = table.Column<double>(type: "float", nullable: true),
                    NumberOfDecimalsForConcentrationBelow10 = table.Column<int>(type: "int", nullable: false),
                    UseLotSpecificCode = table.Column<bool>(type: "bit", nullable: false),
                    DefaultDilutionFactor = table.Column<int>(type: "int", nullable: false),
                    UserDilutionFactor = table.Column<int>(type: "int", nullable: true),
                    DefaultCutOffConcentration = table.Column<double>(type: "float", nullable: true),
                    UserCutOffConcentration = table.Column<double>(type: "float", nullable: true),
                    DefaultLabTestName = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DefaultDilutionMethod = table.Column<int>(type: "int", nullable: false),
                    UserDilutionMethod = table.Column<int>(type: "int", nullable: true),
                    DiluentIdentity = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DefaultEmphasizeOnReports = table.Column<bool>(type: "bit", nullable: false),
                    UserEmphasizeOnReports = table.Column<bool>(type: "bit", nullable: true),
                    TextQuantitativeAbove = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    TextQuantitativeBelow = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    DefaultTextQuantitativeAboveForDilutedTest = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    UserTextQuantitativeAboveForDilutedTest = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    DefaultTextQuantitativeBelowForDilutedTest = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    UserTextQuantitativeBelowForDilutedTest = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    ReportConcentrationAboveCurveFlag = table.Column<bool>(type: "bit", nullable: false),
                    ReportConcentrationBelowCurveFlag = table.Column<bool>(type: "bit", nullable: false),
                    NumberOfDecimalsForConcentrationBetween10And100 = table.Column<int>(type: "int", nullable: false),
                    NumberOfDecimalsForConcentrationAbove100 = table.Column<int>(type: "int", nullable: false),
                    MixOfAllergens = table.Column<bool>(type: "bit", nullable: false),
                    Priority_IncludeInPriorityGroupA = table.Column<bool>(type: "bit", nullable: true),
                    Priority_OrderInPriorityGroupA = table.Column<int>(type: "int", nullable: true),
                    Priority_IncludeInPriorityGroupB = table.Column<bool>(type: "bit", nullable: true),
                    Priority_OrderInPriorityGroupB = table.Column<int>(type: "int", nullable: true),
                    IterativeAlfaBeta1Beta2Flag = table.Column<bool>(type: "bit", nullable: false),
                    AdjustmentForAlfaBeta1Beta2 = table.Column<double>(type: "float", nullable: false),
                    SpecialMethodName = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
                    ConcentrationFactor = table.Column<double>(type: "float", nullable: false),
                    LowerDetectionLimit = table.Column<double>(type: "float", nullable: false),
                    DevelopmentSolutionIdentity = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    IsRuo = table.Column<bool>(type: "bit", nullable: false),
                    IsSpecializedTestActivated = table.Column<bool>(type: "bit", nullable: false),
                    UsageFrequency = table.Column<int>(type: "int", nullable: false),
                    TestNameNaturalSortKey = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true, computedColumnSql: "dbo.NaturalSortKey(TestName)"),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MethodTests_MethodConcentrationUnits_MethodId_DefaultMethodConcentrationUnitId",
                        columns: x => new { x.MethodId, x.DefaultMethodConcentrationUnitId },
                        principalSchema: "Parameters",
                        principalTable: "MethodConcentrationUnits",
                        principalColumns: new[] { "MethodId", "UnitId" });
                    table.ForeignKey(
                        name: "FK_MethodTests_MethodConcentrationUnits_MethodId_UserMethodConcentrationUnitId",
                        columns: x => new { x.MethodId, x.UserMethodConcentrationUnitId },
                        principalSchema: "Parameters",
                        principalTable: "MethodConcentrationUnits",
                        principalColumns: new[] { "MethodId", "UnitId" });
                    table.ForeignKey(
                        name: "FK_MethodTests_Methods_MethodId",
                        column: x => x.MethodId,
                        principalSchema: "Parameters",
                        principalTable: "Methods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MethodCalibrationCurves",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    MethodTestId = table.Column<int>(type: "int", nullable: false),
                    DefaultMaxNumberOfDays = table.Column<int>(type: "int", nullable: false),
                    UserMaxNumberOfDays = table.Column<int>(type: "int", nullable: true),
                    MaxBadReplicates = table.Column<int>(type: "int", nullable: false),
                    QuotientIndex = table.Column<int>(type: "int", nullable: false),
                    ExcludeLowestCalibratorFromMeanDiff = table.Column<bool>(type: "bit", nullable: false),
                    ReportLowRuForZeroCalibrators = table.Column<bool>(type: "bit", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodCalibrationCurves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MethodCalibrationCurves_MethodTests_MethodTestId",
                        column: x => x.MethodTestId,
                        principalSchema: "Parameters",
                        principalTable: "MethodTests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MethodCalibrationCurves_Methods_MethodId",
                        column: x => x.MethodId,
                        principalSchema: "Parameters",
                        principalTable: "Methods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MethodCurveControls",
                schema: "Parameters",
                columns: table => new
                {
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    CurveControlName = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    MethodTestId = table.Column<int>(type: "int", nullable: false),
                    Replicates = table.Column<int>(type: "int", nullable: false),
                    ExpectedConcentration = table.Column<double>(type: "float", nullable: false),
                    ExpectedStdDev = table.Column<double>(type: "float", nullable: false),
                    InnerStdDevLimit = table.Column<double>(type: "float", nullable: false),
                    OuterStdDevLimit = table.Column<double>(type: "float", nullable: false),
                    AdjustmentForAlfaBeta1Beta2 = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodCurveControls", x => new { x.MethodId, x.CurveControlName });
                    table.ForeignKey(
                        name: "FK_MethodCurveControls_MethodTests_MethodTestId",
                        column: x => x.MethodTestId,
                        principalSchema: "Parameters",
                        principalTable: "MethodTests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MethodCurveControls_Methods_MethodId",
                        column: x => x.MethodId,
                        principalSchema: "Parameters",
                        principalTable: "Methods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MethodTestLabNames",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodTestId = table.Column<int>(type: "int", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TestNameNaturalSortKey = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true, computedColumnSql: "dbo.NaturalSortKey(TestName)"),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodTestLabNames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MethodTestLabNames_MethodTests_MethodTestId",
                        column: x => x.MethodTestId,
                        principalSchema: "Parameters",
                        principalTable: "MethodTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PanelTests",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PanelId = table.Column<int>(type: "int", nullable: false),
                    MethodTestId = table.Column<int>(type: "int", nullable: false),
                    Replicates = table.Column<int>(type: "int", nullable: false),
                    InstrumentDilutionFactor = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PanelTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PanelTests_MethodTests_MethodTestId",
                        column: x => x.MethodTestId,
                        principalSchema: "Parameters",
                        principalTable: "MethodTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PanelTests_Panels_PanelId",
                        column: x => x.PanelId,
                        principalSchema: "Parameters",
                        principalTable: "Panels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QualityControlTests",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    QualityControlType = table.Column<int>(type: "int", nullable: false),
                    PreDilutionFactor = table.Column<int>(type: "int", nullable: false),
                    InstrumentDilutionFactor = table.Column<int>(type: "int", nullable: false),
                    Replicates = table.Column<int>(type: "int", nullable: false),
                    ReplicateCvLimit = table.Column<double>(type: "float", nullable: false),
                    MaxCv = table.Column<double>(type: "float", nullable: false),
                    NumberOfSd = table.Column<int>(type: "int", nullable: false),
                    NumberOfTests = table.Column<int>(type: "int", nullable: false),
                    ApplyAcceptanceRange = table.Column<bool>(type: "bit", nullable: false),
                    UseAcceptanceRange = table.Column<bool>(type: "bit", nullable: false),
                    UseLotQualification = table.Column<bool>(type: "bit", nullable: false),
                    SystemType = table.Column<int>(type: "int", nullable: false),
                    TestId = table.Column<int>(type: "int", nullable: false),
                    QualityControlId = table.Column<int>(type: "int", nullable: false),
                    UpperLimit = table.Column<double>(type: "float", nullable: true),
                    LowerLimit = table.Column<double>(type: "float", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityControlTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityControlTests_MethodTests_TestId",
                        column: x => x.TestId,
                        principalSchema: "Parameters",
                        principalTable: "MethodTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualityControlTests_QualityControls_QualityControlId",
                        column: x => x.QualityControlId,
                        principalSchema: "Parameters",
                        principalTable: "QualityControls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MethodCalibrationPoints",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodCalibrationCurveId = table.Column<int>(type: "int", nullable: false),
                    CalibratorName = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ExpectedConcentration = table.Column<double>(type: "float", nullable: false),
                    Replicates = table.Column<int>(type: "int", nullable: false),
                    AdjustmentForAlfaBeta1Beta2 = table.Column<double>(type: "float", nullable: false),
                    IdealResponse = table.Column<double>(type: "float", nullable: false),
                    ExpectedStdDev = table.Column<double>(type: "float", nullable: false),
                    UpperResponseLimit = table.Column<double>(type: "float", nullable: false),
                    LowerResponseLimit = table.Column<double>(type: "float", nullable: false),
                    StdDevLimit = table.Column<double>(type: "float", nullable: false),
                    StdDevPrecisionLimit = table.Column<double>(type: "float", nullable: false),
                    UpperLogarithmicLimit = table.Column<double>(type: "float", nullable: true),
                    LowerLogarithmicLimit = table.Column<double>(type: "float", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodCalibrationPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MethodCalibrationPoints_MethodCalibrationCurves_MethodCalibrationCurveId",
                        column: x => x.MethodCalibrationCurveId,
                        principalSchema: "Parameters",
                        principalTable: "MethodCalibrationCurves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MethodQualityControlLot",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodQualityControlId = table.Column<int>(type: "int", nullable: false),
                    RangeMin = table.Column<double>(type: "float", maxLength: 100000, nullable: true),
                    RangeMax = table.Column<double>(type: "float", maxLength: 100000, nullable: true),
                    ExcludeRangeMax = table.Column<bool>(type: "bit", nullable: false),
                    ExcludeRangeMin = table.Column<bool>(type: "bit", nullable: false),
                    ManufacturingRangeMin = table.Column<double>(type: "float", maxLength: 100000, nullable: true),
                    ManufacturingRangeMax = table.Column<double>(type: "float", maxLength: 100000, nullable: true),
                    ExpectedConcentration = table.Column<double>(type: "float", maxLength: 100000, nullable: true),
                    ManufacturingExpectedConcentration = table.Column<double>(type: "float", maxLength: 100000, nullable: true),
                    ExpectedStdDev = table.Column<double>(type: "float", maxLength: 100000, nullable: true),
                    StdDevLimit = table.Column<double>(type: "float", maxLength: 1000, nullable: true),
                    LotNumber = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    ExpirationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LotType = table.Column<int>(type: "int", nullable: false),
                    ManufacturingLimitCheck = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodQualityControlLot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MethodQualityControlLot_QualityControlTests_MethodQualityControlId",
                        column: x => x.MethodQualityControlId,
                        principalSchema: "Parameters",
                        principalTable: "QualityControlTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QualityControlSchedules",
                schema: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstInRun = table.Column<bool>(type: "bit", nullable: false),
                    LastInRun = table.Column<bool>(type: "bit", nullable: false),
                    AfterCalibration = table.Column<bool>(type: "bit", nullable: false),
                    Periodically = table.Column<int>(type: "int", nullable: false),
                    StartAfter = table.Column<int>(type: "int", nullable: false),
                    RunEvery = table.Column<int>(type: "int", nullable: false),
                    QualityControlTestId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityControlSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityControlSchedules_QualityControlTests_QualityControlTestId",
                        column: x => x.QualityControlTestId,
                        principalSchema: "Parameters",
                        principalTable: "QualityControlTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ArticleNumber",
                schema: "Parameters",
                table: "Articles",
                column: "ArticleNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ArticleType",
                schema: "Parameters",
                table: "Articles",
                column: "ArticleType");

            migrationBuilder.CreateIndex(
                name: "IX_MethodCalibrationCurves_MethodId",
                schema: "Parameters",
                table: "MethodCalibrationCurves",
                column: "MethodId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MethodCalibrationCurves_MethodTestId",
                schema: "Parameters",
                table: "MethodCalibrationCurves",
                column: "MethodTestId");

            migrationBuilder.CreateIndex(
                name: "IX_MethodCalibrationPoints_MethodCalibrationCurveId_CalibratorName",
                schema: "Parameters",
                table: "MethodCalibrationPoints",
                columns: new[] { "MethodCalibrationCurveId", "CalibratorName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MethodClassTableBelows_MethodId_ClassTableId",
                schema: "Parameters",
                table: "MethodClassTableBelows",
                columns: new[] { "MethodId", "ClassTableId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MethodClassTables_MethodId_ClassTableId_ClassTableRowId",
                schema: "Parameters",
                table: "MethodClassTables",
                columns: new[] { "MethodId", "ClassTableId", "ClassTableRowId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MethodCurveControls_MethodId_CurveControlName",
                schema: "Parameters",
                table: "MethodCurveControls",
                columns: new[] { "MethodId", "CurveControlName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MethodCurveControls_MethodTestId",
                schema: "Parameters",
                table: "MethodCurveControls",
                column: "MethodTestId");

            migrationBuilder.CreateIndex(
                name: "IX_MethodQualityControlLot_LotNumber",
                schema: "Parameters",
                table: "MethodQualityControlLot",
                column: "LotNumber");

            migrationBuilder.CreateIndex(
                name: "IX_MethodQualityControlLot_MethodQualityControlId",
                schema: "Parameters",
                table: "MethodQualityControlLot",
                column: "MethodQualityControlId");

            migrationBuilder.CreateIndex(
                name: "IX_MethodReagents_MethodId_ReagentType",
                schema: "Parameters",
                table: "MethodReagents",
                columns: new[] { "MethodId", "ReagentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Methods_MethodName",
                schema: "Parameters",
                table: "Methods",
                column: "MethodName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Methods_MethodNameLis",
                schema: "Parameters",
                table: "Methods",
                column: "MethodNameLis",
                unique: true,
                filter: "[MethodNameLis] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Methods_SortOrder",
                schema: "Parameters",
                table: "Methods",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_MethodTestLabNames_MethodTestId_TestName",
                schema: "Parameters",
                table: "MethodTestLabNames",
                columns: new[] { "MethodTestId", "TestName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MethodTests_MethodId_DefaultMethodConcentrationUnitId",
                schema: "Parameters",
                table: "MethodTests",
                columns: new[] { "MethodId", "DefaultMethodConcentrationUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_MethodTests_MethodId_TestName",
                schema: "Parameters",
                table: "MethodTests",
                columns: new[] { "MethodId", "TestName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MethodTests_MethodId_UserMethodConcentrationUnitId",
                schema: "Parameters",
                table: "MethodTests",
                columns: new[] { "MethodId", "UserMethodConcentrationUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_MethodTests_TestName",
                schema: "Parameters",
                table: "MethodTests",
                column: "TestName");

            migrationBuilder.CreateIndex(
                name: "IX_PackageItems_ArticleId",
                schema: "Parameters",
                table: "PackageItems",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageItems_PackageId_ArticleId",
                schema: "Parameters",
                table: "PackageItems",
                columns: new[] { "PackageId", "ArticleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Packages_Hidden",
                schema: "Parameters",
                table: "Packages",
                column: "Hidden");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_IntendedMarketId",
                schema: "Parameters",
                table: "Packages",
                column: "IntendedMarketId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_PackageBarcode",
                schema: "Parameters",
                table: "Packages",
                column: "PackageBarcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Panels_PanelName",
                schema: "Parameters",
                table: "Panels",
                column: "PanelName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PanelTests_MethodTestId",
                schema: "Parameters",
                table: "PanelTests",
                column: "MethodTestId");

            migrationBuilder.CreateIndex(
                name: "IX_PanelTests_PanelId_MethodTestId",
                schema: "Parameters",
                table: "PanelTests",
                columns: new[] { "PanelId", "MethodTestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityControls_BarcodeFormatId",
                schema: "Parameters",
                table: "QualityControls",
                column: "BarcodeFormatId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityControls_Name",
                schema: "Parameters",
                table: "QualityControls",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_QualityControlSchedules_QualityControlTestId",
                schema: "Parameters",
                table: "QualityControlSchedules",
                column: "QualityControlTestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityControlTests_QualityControlId",
                schema: "Parameters",
                table: "QualityControlTests",
                column: "QualityControlId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityControlTests_TestId",
                schema: "Parameters",
                table: "QualityControlTests",
                column: "TestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticlesVersion",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "CartridgeCellIdentity",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "MethodCalibrationPoints",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "MethodClassTableBelows",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "MethodClassTables",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "MethodCurveControls",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "MethodQualityControlLot",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "MethodReagents",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "MethodTestLabNames",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "PackageItems",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "PanelTests",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "ParameterUpdatePackageVersion",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "QualityControlSchedules",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "MethodCalibrationCurves",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "Articles",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "Packages",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "Panels",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "QualityControlTests",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "IntendedMarkets",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "MethodTests",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "QualityControls",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "MethodConcentrationUnits",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "QualityControlBarcodeFormats",
                schema: "Parameters");

            migrationBuilder.DropTable(
                name: "Methods",
                schema: "Parameters");
        }
    }
}
