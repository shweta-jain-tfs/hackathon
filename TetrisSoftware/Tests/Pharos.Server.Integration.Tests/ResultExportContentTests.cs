using System.Globalization;
using System.Text;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetResultExport;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class ResultExportContentTests : IntegrationTestBase
{
    private const string _articleNumber = "C4K"; //From ImmunoCap Reagent barcode
    private const string _devSolutionBarcode = _articleNumber + "XXXNZ0001";
    private const string _instrumentNumber = "SN001";
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private HttpClient _client;
    private List<string> _expectedFileContent;
    private readonly Timestamp _timeStamp;
    private readonly string _timeAsString;

    public ResultExportContentTests(IntegrationTestsWaf factory) : base(factory)
    {
        var timeStamp = DateTimeOffset.Parse("2023-09-29 09:17:00", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        _timeStamp = Timestamp.FromDateTimeOffset(timeStamp);
        _timeAsString = timeStamp.ToLocalTime().DateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _client = Factory.HttpClient;
        var versionService = ServiceProvider.GetRequiredService<IPharosVersionService>();
        _expectedFileContent = CreateExpectedResultFile(versionService.Version);
    }

    private List<string> CreateExpectedResultFile(string version)
    {
        return [
        "sep=;",
        "[FILE]",
        "Type=ASSAY RESULT DATA FILE",
        "FormatVersion=1.0",
        "[INFORMATION]",
        "TypeOfExport;StartDate;StartTime;EndDate;EndTime;AnalyticalRunId;CustomRunId;InstrumentName;InstrumentSerialNumber;PharosVersion;InstrumentType;ExportByUser",
        "1;2023-09-29;" + _timeAsString + ";;;00000000-0000-0000-0000-000000000001;;;SN001;" + version + ";;",
        "[RESULTS]",
        "Seq;ReplicateId;MeasDate;MeasTime;Status;SampleId;Request;MethodName;TestName;SampleType;CarrierLotNumber;CarrierSerialNumber;CarrierExpired;CALCCQCLotNumber;CALCCQCSerialNumber;CALCCQCExpired;DiluentLotNumber;DiluentSerialNumber;DiluentExpired;ConjugateLotNumber;ConjugateSerialNumber;ConjugateExpired;DevelopmentLotNumber;DevelopmentSerialNumber;DevelopmentExpired;StopLotNumber;StopSerialNumber;StopExpired;Stop2LotNumber;Stop2SerialNumber;Stop2Expired;WashConcLotNumber;WashConcSerialNumber;WashConcExpired;WashAddLotNumber;WashAddSerialNumber;WashAddExpired;WashConc2LotNumber;WashConc2SerialNumber;WashConc2Expired;WashAdd2LotNumber;WashAdd2SerialNumber;WashAdd2Expired;RU;Low RU;Conc;Quot;Class;Cut-off;Errors;PreDil;InsDil;AnalyticalRunId;Comment;FlMeasurementValue;Blank;CustomRunId;CmrTestId;CurveId;CalcCurveId;GroupId;OrgResp;EditResp;RespEdited;Judgement;ErrorCodes;IRWSpoke;IRWRow;ERWSpoke;ERWRow",
        "1;00000000-0000-0000-0000-000000000ca1;2023-09-29;" + _timeAsString + ";1;name;;sIgE;c1;2;A50XN;1244;0;A4KNZ;0001;0;;;0;;;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;125;0;0;;;;0;1;1;00000000-0000-0000-0000-000000000001;;15;1;;;1;0;00000000-0000-0000-0000-000000000ca2;125;;0;0;;0;0;0;0",
        "2;00000000-0000-0000-0000-000000000cc1;2023-09-29;" + _timeAsString + ";1;name;;sIgE;c1;3;A6JXN;1244;0;A4KNZ;0001;0;;;0;;;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;125;0;0;;;;0;1;1;00000000-0000-0000-0000-000000000001;;15;1;;;0;0;00000000-0000-0000-0000-000000000cc2;0;;0;0;;0;0;0;0",
        "3;00000000-0000-0000-0000-000000000bc1;2023-09-29;" + _timeAsString + ";1;name;;sIgE;c1;4;BL1XN;1244;0;A4KNZ;0001;0;;;0;;;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;125;0;0;;;;0;1;1;00000000-0000-0000-0000-000000000001;;15;1;;;0;0;00000000-0000-0000-0000-000000000bc2;0;;0;0;;0;0;0;0",
        "4;00000000-0000-0000-0000-000000000aa2;2023-09-29;" + _timeAsString + ";1;B4KXXXNZ00011;;sIgE;c1;1;A4KXN;1234;0;;;0;;;0;;;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;125;0;0;;;;0;1;1;00000000-0000-0000-0000-000000000001;;15;1;;;0;0;;0;;0;0;;0;0;0;0",
        "[MEAN]",
        "Seq;SampleType;GroupId;SampleId;MethodName;TestName;Resp;Conc;%CVResp;%CVConc",
        "1;2;00000000-0000-0000-0000-000000000ca2;name;sIgE;c1;0;0;0;0",
        "2;3;00000000-0000-0000-0000-000000000cc2;name;sIgE;c1;0;0;0;0",
        "3;4;00000000-0000-0000-0000-000000000bc2;name;sIgE;c1;0;0;0;0",
        "4;1;;B4KXXXNZ00011;sIgE;c1;0;0;0;0"
        ];
    }


    [Fact]
    public async Task GetAnalyticalRunsExport()
    {
        //Arrange
        var analyticalRunId = await SendTestResultOnMediator();
        var exportQuery = new GetResultExportQuery([analyticalRunId]);

        //Act
        using var createContent = new StringContent(JsonSerializer.Serialize(exportQuery), Encoding.UTF8, "application/json");
        using var resultMessage = await _client.PostAsync(new Uri(Factory.Server.BaseAddress, "api/results/export"), createContent, TestContext.Current.CancellationToken);

        //Assert
        _ = resultMessage.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        _ = resultMessage.Content.Headers.Should().ContainKey("Content-Disposition");
        var actualFileContent = await GetContentStringFromMessage(resultMessage);
        AssertStringArrays(_expectedFileContent, actualFileContent);
    }

    private static async Task<List<string>> GetContentStringFromMessage(HttpResponseMessage actualContent)
    {
        var content = await actualContent.Content.ReadAsByteArrayAsync();

        using (var actualStream = new MemoryStream(content))
        {
            using (var actualStreamReader = new StreamReader(actualStream))
            {
                var str = actualStreamReader.ReadToEnd();
                return [.. str.Split("\r\n")];
            }
        }
    }

    private static void AssertStringArrays(List<string> expectedArray, List<string> actualArray)
    {
        _ = actualArray.RemoveAll(string.IsNullOrWhiteSpace);

        _ = actualArray.Count.Should().Be(expectedArray.Count);
        for (var i = 0; i < actualArray.Count; i++)
        {
            _ = actualArray[i].Length.Should().Be(expectedArray[i].Length);
            _ = actualArray[i].Should().Be(expectedArray[i]);
        }
    }

    private async Task<int> SendTestResultOnMediator()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var paramDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var analyticalRunId = new Guid("00000000-0000-0000-0000-000000000001");
        var assayRunId = new Guid("00000000-0000-0000-0000-000000000002");
        var calibratorReplicateId = new Guid("00000000-0000-0000-0000-000000000ca1");
        var calibratorGroupId = new Guid("00000000-0000-0000-0000-000000000ca2");
        var ccReplicateId = new Guid("00000000-0000-0000-0000-000000000cc1");
        var ccGroupId = new Guid("00000000-0000-0000-0000-000000000cc2");
        var qcReplicateId = new Guid("00000000-0000-0000-0000-000000000bc1");
        var qcGroupId = new Guid("00000000-0000-0000-0000-000000000bc2");
        var sampleGroupId = new Guid("00000000-0000-0000-0000-000000000aa1");
        var sampleReplicateId = new Guid("00000000-0000-0000-0000-000000000aa2");

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        await PublishCalibrator(mediator, paramDbContext, calibratorReplicateId, analyticalRunId, calibratorGroupId, assayRunId);
        await PublishCurveControl(mediator, paramDbContext, ccReplicateId, analyticalRunId, ccGroupId, assayRunId);
        await PublishQualityControl(mediator, paramDbContext, qcReplicateId, analyticalRunId, qcGroupId, assayRunId);
        await PublishSample(mediator, sampleReplicateId, analyticalRunId, sampleGroupId, assayRunId);

        var testResult = await dbContext.TestResults.Where(x => x.Replicates!.Any(r => r.InstrumentReplicateId == calibratorReplicateId))
            .Include(x => x.Replicates!)
            .SingleOrDefaultAsync(TestContext.Current.CancellationToken);

        testResult.Should().NotBeNull();
        testResult.Replicates.Should().NotBeNullOrEmpty();
        testResult.Replicates!.Count.Should().Be(1);

        return testResult.AnalyticalRunId!.Value;
    }

    private async Task PublishCalibrator(IMediator mediator, IParametersDbContext dbContext, Guid instrumentReplicateId, Guid analyticalRunId, Guid groupId, Guid assayRunId)
    {
        var articleNumber = "A50";
        await SeedArticle(dbContext, articleNumber, ArticleType.CalibratorCartridge);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.Calibrator, EnumReagentType.CalibratorCartridge, analyticalRunId, groupId, articleNumber, assayRunId));
        await mediator.Send(notification, TestContext.Current.CancellationToken);
    }

    private async Task PublishCurveControl(IMediator mediator, IParametersDbContext dbContext, Guid instrumentReplicateId, Guid analyticalRunId, Guid groupId, Guid assayRunId)
    {
        var articleNumber = "A6J";
        await SeedArticle(dbContext, articleNumber, ArticleType.CurveControlCartridge);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.CurveControl, EnumReagentType.CurveControlCartridge, analyticalRunId, groupId, articleNumber, assayRunId));
        await mediator.Send(notification, TestContext.Current.CancellationToken);
    }

    private async Task PublishQualityControl(IMediator mediator, IParametersDbContext dbContext, Guid instrumentReplicateId, Guid analyticalRunId, Guid groupId, Guid assayRunId)
    {
        var articleNumber = "BL1";
        await SeedArticle(dbContext, articleNumber, ArticleType.QualityControlCartridge);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.QualityControl, EnumReagentType.QualityControlCartridge, analyticalRunId, groupId, articleNumber, assayRunId));
        await mediator.Send(notification, TestContext.Current.CancellationToken);
    }

    private async Task PublishSample(IMediator mediator, Guid instrumentReplicateId, Guid analyticalRunId, Guid groupId, Guid assayRunId)
    {
        var articleNumber = "g10";
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.Sample, EnumReagentType.ImmunoCapCarrier, analyticalRunId, groupId, articleNumber, assayRunId));
        await mediator.Send(notification, TestContext.Current.CancellationToken);
    }

    internal static Task SendBlankResult(IMediator mediator, string instrumentNumber, string devSolutionBarcode, int cellPosition = 1, bool isOk = true)
    {
        var blankResult = new BlankResult
        {
            BlankType = EnumBlankType.ReagentImmunoCap,
            ResultOk = isOk,
            MeanValue = 10,
            UpperLimit = 9999,
            LowerLimit = 0,
            DevelopmentSolutionIdentity = new DevelopmentSolutionIdentity
            {
                ReagentBarcode = devSolutionBarcode,
                CellPosition = cellPosition,
            },
            StopSolutionBarcode = "DoesNotMatter"
        };

        var notification = new ProcessBlankResultCommand(instrumentNumber, blankResult);
        return mediator.Send(notification, TestContext.Current.CancellationToken);
    }

    internal static async Task SeedArticle(IParametersDbContext dbContext, string articleNumber, ArticleType articleType)
    {
        var article = new Article
        {
            ArticleNumber = articleNumber,
            ArticleType = articleType,
            Quantity = 1,
            MaxDaysInInstrument = 99,
        };
        dbContext.Articles.Add(article);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public AssayResult CreateAssayMessage(Guid instrumentReplicateId, EnumTestType testType, EnumReagentType reagentType, Guid analyticalRunId, Guid groupId, string articleNumber, Guid assayRunId)
    {
        var sampleReagent = new ReagentInformation
        {
            ReagentType = reagentType,
            ReagentBarcode = "sampleReagCode",
            ReagentUdiBarcode = "CarrierBottleUdi",
            CellPosition = 1,
            ExpiredReagent = false
        };

        var reagent1 = new ReagentInformation
        {
            ReagentType = reagentType,
            ReagentBarcode = "A4KXXXNZ0001",
            ReagentUdiBarcode = "|01510222222333361715070710A213B|991234",
            CellPosition = 1,
            ExpiredReagent = false
        };

        var reagent2 = new ReagentInformation
        {
            ReagentType = EnumReagentType.ImmunoCapCarrier,
            ReagentBarcode = articleNumber + "XXXNZ0001",
            ReagentUdiBarcode = "|01510222222333361715070710A213B|991244",
            CellPosition = 1,
            ExpiredReagent = false
        };

        var reagent3 = new ReagentInformation
        {
            ReagentType = EnumReagentType.DevelopmentSolutionCartridge,
            ReagentBarcode = _devSolutionBarcode,
            CellPosition = 1,
            ExpiredReagent = false
        };

        var assay = new AssayResult
        {
            TestType = testType,
            TimeOfMeasurement = _timeStamp,
            MethodName = "sIgE",
            TestName = "c1",
            InstrumentDilutionFactor = 1,

            Sample = new SampleInformation
            {
                SampleDate = _timeStamp,
                PreDilutionFactor = 1
            },

            Replicate = new ReplicateInfo
            {
                ReplicateId = instrumentReplicateId.ToString(),
                GroupId = groupId.ToString(),
                ReplicatesInTestOrder = 2,
            },

            Run = new RunInfo
            {
                AnalyticalRunId = analyticalRunId.ToString(),
                AssayRunId = assayRunId.ToString()
            },

            FluorometerResult = new FluorometerValue
            {
                MeasurementValue = 15

            },

            Reagents =
            {
                reagent1,
                reagent2,
                reagent3
            }
        };

        if (testType != EnumTestType.Sample)
        {
            assay.Reagents.Add(sampleReagent);
            assay.Sample.SampleId = "name";
        }
        else
        {
            assay.Sample.SampleId = "B4KXXXNZ00011";
        }
        return assay;
    }
}
