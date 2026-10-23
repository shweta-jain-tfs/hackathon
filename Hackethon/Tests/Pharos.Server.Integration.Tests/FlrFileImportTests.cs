using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetResultExport;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class FlrFileImportTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    // TODO: Fix test import data for CC so it is consistent with the updated judgment logic, then enable the test with both minimum and all data.
    [Theory(Skip = "Test temporarily disabled - judgment on cc has been updated and import data need to be validated - CC only valid from the curve measure date to the committedToCalculateAt date")]
    [InlineData(true)]
    //[InlineData(false)]
    public async Task ImportTestAllData(bool useAllData)
    {
        var client = Factory.HttpClient;

        await using (var asyncScope = Factory.Services.CreateAsyncScope())
        {
            var parametersDb = asyncScope.ServiceProvider.GetService<IParametersDbContext>()!;
            await InitParameters(parametersDb);

            // Create IFormFile from string content
            var fileContent = useAllData ? _allData : _minimumData;
            var fileName = "import.flr";

            // Prepare multipart form data content for file upload
            using var multipartContent = new MultipartFormDataContent();
            using var streamContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(fileContent)));
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

            multipartContent.Add(streamContent, "file", fileName);

            var uri = new Uri(Factory.Server.BaseAddress, "/api/flrImport");
            var result = await client.PostAsync(uri, multipartContent, TestContext.Current.CancellationToken);

            result.EnsureSuccessStatusCode();

            // Polling to check if import is completed
            for (var i = 0; i < 100; i++)
            {
                result = await client.GetAsync(uri, TestContext.Current.CancellationToken);
                var response = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                if (!bool.Parse(response))
                {
                    break;
                }

                if (i == 99)
                {
                    Assert.Fail("FLR import did not complete within the expected time.");
                }

                // Wait for a short time before checking again
                await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
            }
        }

        await WaitForNotificationsAsync();

        // Assert
        var requestDb = ServiceProvider.GetService<IRequestDbContext>()!;
        var resultDb = ServiceProvider.GetService<IResultDbContext>()!;

        await CheckTestOrders(requestDb);
        await CheckCurve(resultDb);
        await CheckCurveControls(resultDb);
        await CheckQualityControls(resultDb);
        await CheckTestResults(resultDb);

        if (useAllData)
        {
            // Export and verify file content
            var exportQuery = new GetResultExportQuery([1]);
            using var createContent = new StringContent(JsonSerializer.Serialize(exportQuery), Encoding.UTF8, "application/json");
            using var exportResponse = await client.PostAsync(new Uri(Factory.Server.BaseAddress, "api/results/export"), createContent, TestContext.Current.CancellationToken);

            exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            exportResponse.Content.Headers.Should().ContainKey("Content-Disposition");
            var actualFileContent = await GetContentStringFromMessage(exportResponse);

            AssertStringArrays(GetExpectedAllDataFileContent(), actualFileContent);
        }
    }

    private static List<string> GetExpectedAllDataFileContent()
    {
        const string expected = """
                                sep=;
                                [FILE]
                                Type=ASSAY RESULT DATA FILE
                                FormatVersion=1.0

                                [INFORMATION]
                                TypeOfExport;StartDate;StartTime;EndDate;EndTime;AnalyticalRunId;CustomRunId;InstrumentName;InstrumentSerialNumber;PharosVersion;InstrumentType;ExportByUser
                                1;2024-10-09;09:05:07;2025-11-14;12:53:17;267d4ee0-072f-4697-9d72-95afd85e7a6f;;;SEED_001;1.0.0.0;;00000000-0000-0000-0000-000000000000

                                [RESULTS]
                                Seq;ReplicateId;MeasDate;MeasTime;Status;SampleId;Request;MethodName;TestName;SampleType;CarrierLotNumber;CarrierSerialNumber;CarrierExpired;CALCCQCLotnumber;CALCCQCSerialNumber;CALCCQCExpired;DiluentLotNumber;DiluentSerialNumber;DiluentExpired;ConjugateLotNumber;ConjugateSerialNumber;ConjugateExpired;DevelopmentLotNumber;DevelopmentSerialNumber;DevelopentExpired;StopLotNumber;StopSerialNumber;StopExpired;Stop2LotNumber;Stop2SerialNumber;Stop2Expired;WashConcLotNumber;WashConcSerialNumber;WashConcExpired;WashAddLotNumber;WashAddSerialNumber;WashAddExpired;WashConc2LotNumber;WashConc2SerialNumber;WashConc2Expired;WashAdd2LotNumber;WashAdd2SerialNumber;WashAdd2Expired;RU;Low RU;Conc;Quot;Class;Cut-off;Errors;PreDil;InsDil;AnalyticalRunId;Comment;FlMeasurementValue;Blank;CustomRunId;CmrTestId;CurveId;CalcCurveId;GroupId;OrgResp;EditResp;RespEdited;Judgement;ErrorCodes;IRWSpoke;IRWRow;ERWSpoke;ERWRow
                                1;f3845fc2-6b02-4ca5-abf6-c244a42cd24e;2024-10-09;09:05:07;1;CAL-0.00;;sIgE;a_IgE;2;;;0;A5SSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;36.35;0;0;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;1592;1;;;1;1;91f40f1d-a848-4580-80cc-898c1bb77d16;36.35;;0;2;;1;1;1;1
                                2;3fb24d3e-482d-4d71-8a32-28c22c88fadf;2024-10-09;09:05:07;1;CAL-0.00;;sIgE;a_IgE;2;;;0;A5SSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;36.5;0;0;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;1598;1;;;1;1;91f40f1d-a848-4580-80cc-898c1bb77d16;36.5;;0;2;;1;1;1;1
                                3;94e35bb0-443d-464b-ab18-eefa7559b646;2024-10-09;09:05:07;1;CAL-0.35;;sIgE;a_IgE;2;;;0;A5SSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;217.375;0;0.3500529468289305;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;8833;1;;;1;1;91f40f1d-a848-4580-80cc-898c1bb77d16;217.375;;0;2;;1;1;1;1
                                4;2ffef76f-e7f5-47bb-b1b0-f839462f797a;2024-10-09;09:05:07;1;CAL-0.35;;sIgE;a_IgE;2;;;0;A5SSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;217.325;0;0.34995472791597376;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;8831;1;;;1;1;91f40f1d-a848-4580-80cc-898c1bb77d16;217.325;;0;2;;1;1;1;1
                                5;18fa9a29-bb87-4b9d-a665-8a5131e1350b;2024-10-09;09:05:07;1;CAL-0.70;;sIgE;a_IgE;2;;;0;A5SSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;412.29999999999995;0;0.7285277785735695;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;16630;1;;;1;1;91f40f1d-a848-4580-80cc-898c1bb77d16;412.29999999999995;;0;2;;1;1;1;1
                                6;15240546-2e1e-44c6-b4e0-f59b8b0b3ae7;2024-10-09;09:05:07;1;CAL-0.70;;sIgE;a_IgE;2;;;0;A5SSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;412.325;0;0.7285759494994792;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;16631;1;;;1;1;91f40f1d-a848-4580-80cc-898c1bb77d16;412.325;;0;2;;1;1;1;1
                                7;d2ac77e2-a6d6-4aef-887c-beba0f1c1891;2024-10-09;09:05:07;1;CAL-3.50;;sIgE;a_IgE;2;;;0;A5SSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;1836.3249999999998;0;3.4868665604559115;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;73591;1;;;1;1;91f40f1d-a848-4580-80cc-898c1bb77d16;1836.3249999999998;;0;2;;1;1;1;1
                                8;09c06bda-fb44-4c7d-af55-289d5cf2c16b;2024-10-09;09:05:07;1;CAL-3.50;;sIgE;a_IgE;2;;;0;A5SSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;1836.375;0;3.486965650704146;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;73593;1;;;1;1;91f40f1d-a848-4580-80cc-898c1bb77d16;1836.375;;0;2;;1;1;1;1
                                9;35427311-930e-4828-aa18-7e260e73405d;2024-10-09;09:05:07;1;CAL-17.5;;sIgE;a_IgE;2;;;0;A5SSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;7716.400000000001;0;17.503318985608384;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;308794;1;;;1;1;91f40f1d-a848-4580-80cc-898c1bb77d16;7716.400000000001;;0;2;;1;1;1;1
                                10;e5b0df3d-2fe7-4685-bb49-09b2085d38f0;2024-10-09;09:05:07;1;CAL-17.5;;sIgE;a_IgE;2;;;0;A5SSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;7716.500000000001;0;17.503611098218563;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;308798;1;;;1;1;91f40f1d-a848-4580-80cc-898c1bb77d16;7716.500000000001;;0;2;;1;1;1;1
                                11;cf526a63-9090-450b-8597-3d6a9a2e2eaa;2024-10-09;09:05:07;1;CAL-100;;sIgE;a_IgE;2;;;0;A5SSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;20608.374999999996;0;99.99870604938876;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;824473;1;;;1;1;91f40f1d-a848-4580-80cc-898c1bb77d16;20608.374999999996;;0;2;;1;1;1;1
                                12;65e39090-c01c-4220-856f-704bf96824c7;2024-10-09;09:05:07;1;CAL-100;;sIgE;a_IgE;2;;;0;A5SSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;20608.325;0;99.99801732666515;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;824471;1;;;1;1;91f40f1d-a848-4580-80cc-898c1bb77d16;20608.325;;0;2;;1;1;1;1
                                13;68588234-a3d4-4c13-9321-4c5479eba52a;2024-10-09;09:05:08;1;CC-1;;sIgE;a_IgE;3;;;0;A0VSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;412.47499999999997;0;0.7288649737984695;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;16637;1;;;0;1;86c3167b-4a33-44f3-a900-d67ad7f1a65a;0;;0;1;;1;1;1;1
                                14;de95c413-1ece-40ae-a0bc-9ec50a489192;2024-10-09;09:05:08;1;CC-2;;sIgE;a_IgE;3;;;0;A0VSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;7716.475000000001;0;17.50353806984591;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;308797;1;;;0;1;86c3167b-4a33-44f3-a900-d67ad7f1a65a;0;;0;1;;1;1;1;1
                                15;e3e9e4ab-ade9-413a-8a49-36115d7b3065;2024-10-09;09:05:09;1;sIgE Pos;;sIgE;d1;4;;;0;AVMSS;0001;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;998.9499999999999;0;1.8543280894255185;;;;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;40096;1;;;0;1;46f1eeb2-4d8b-4fc0-a47c-5af72d1ca4f2;0;;0;1;;1;1;1;1
                                16;d6e464e0-eb5e-4c4a-9388-bd4d5b03e887;2024-10-09;09:05:11;1;BC-KBQWQ1;;sIgE;t1;1;;;0;;;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;1682.1;0;3.182256156138699;;---;---;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;67422;1;;;0;1;;0;;0;0;;1;1;1;1
                                17;a96e46fd-41d4-43e3-ab5c-f288d3ade465;2024-10-09;09:05:12;1;BC-KBQWQ1;;sIgE;t2;1;;;0;;;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;1281.3;0;2.3994689287176616;;---;---;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;51390;1;;;0;1;;0;;0;0;;1;1;1;1
                                18;a72297c6-e829-4e90-b5b2-5d179a692560;2024-10-09;09:05:12;1;BC-KBQWQ1;;sIgE;t3;1;;;0;;;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;1436.7250000000001;0;2.7016209750982725;;---;---;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;57607;1;;;0;1;;0;;0;0;;1;1;1;1
                                19;8c357b4c-f7d6-48a2-9a88-1a9c38c9f219;2024-10-09;09:05:12;1;BC-KBQWQ1;;sIgE;c1;1;;;0;;;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;1611.3249999999998;0;3.043137589631223;;---;---;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;64591;1;;;0;1;;0;;0;0;;1;1;1;1
                                20;d03a6602-81d9-4499-87ff-485b576b282d;2024-10-09;09:05:12;1;BC-KBQWQ1;;sIgE;c2;1;;;0;;;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;1743.825;0;3.3039246464245755;;---;---;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;69891;1;;;0;1;;0;;0;0;;1;1;1;1
                                21;965098e6-65b2-4260-93c9-bb0385546516;2024-10-09;09:05:12;1;BC-KBQWQ1;;sIgE;c5;1;;;0;;;0;;;0;A3TEM;0001;0;C4KNZ;0001;0;;;0;;;0;;;0;;;0;;;0;;;0;1347.175;0;2.527331871913796;;---;---;0;1;1;267d4ee0-072f-4697-9d72-95afd85e7a6f;;54025;1;;;0;1;;0;;0;0;;1;1;1;1

                                [MEAN]
                                Seq;SampleType;GroupId;SampleId;MethodName;TestName;Resp;Conc;%CVResp;%CVConc
                                1;2;91f40f1d-a848-4580-80cc-898c1bb77d16;CAL-0.00;sIgE;a_IgE;36.425;0;0.29119016384409496;0
                                2;2;91f40f1d-a848-4580-80cc-898c1bb77d16;CAL-0.35;sIgE;a_IgE;217.35;0.3500038373724521;0.01626654652403765;0.019842999452484533
                                3;2;91f40f1d-a848-4580-80cc-898c1bb77d16;CAL-0.70;sIgE;a_IgE;412.3125;0.7285518640365244;0.00428744403474039;0.004675300381669413
                                4;2;91f40f1d-a848-4580-80cc-898c1bb77d16;CAL-3.50;sIgE;a_IgE;1836.35;3.486916105580029;0.0019253048368490725;0.0020094362964912533
                                5;2;91f40f1d-a848-4580-80cc-898c1bb77d16;CAL-17.5;sIgE;a_IgE;7716.450000000001;17.503465041913472;0.0009163623739046828;0.0011800821558730105
                                6;2;91f40f1d-a848-4580-80cc-898c1bb77d16;CAL-100;sIgE;a_IgE;20608.35;99.99836168802696;0.00017155210277346582;0.00048700604273187534
                                7;3;86c3167b-4a33-44f3-a900-d67ad7f1a65a;CC-1;sIgE;a_IgE;412.47499999999997;0.7288649737984695;0;0
                                8;3;86c3167b-4a33-44f3-a900-d67ad7f1a65a;CC-2;sIgE;a_IgE;7716.475000000001;17.50353806984591;0;0
                                9;4;46f1eeb2-4d8b-4fc0-a47c-5af72d1ca4f2;sIgE Pos;sIgE;d1;998.9499999999999;1.8543280894255185;0;0
                                10;1;;BC-KBQWQ1;sIgE;t1;1682.1;3.182256156138699;0;0
                                11;1;;BC-KBQWQ1;sIgE;t2;1281.3;2.3994689287176616;0;0
                                12;1;;BC-KBQWQ1;sIgE;t3;1436.7250000000001;2.7016209750982725;0;0
                                13;1;;BC-KBQWQ1;sIgE;c1;1611.3249999999998;3.043137589631223;0;0
                                14;1;;BC-KBQWQ1;sIgE;c2;1743.825;3.3039246464245755;0;0
                                15;1;;BC-KBQWQ1;sIgE;c5;1347.175;2.527331871913796;0;0
                                """;


        return expected.Split("\r\n").ToList();
    }

    private static async Task<List<string>> GetContentStringFromMessage(HttpResponseMessage actualContent)
    {
        var content = await actualContent.Content.ReadAsByteArrayAsync();

        using var actualStream = new MemoryStream(content);
        using var actualStreamReader = new StreamReader(actualStream);
        var str = await actualStreamReader.ReadToEndAsync();
        return str.Split("\r\n").ToList();
    }

    private static void AssertStringArrays(List<string> expectedArray, List<string> actualArray)
    {
        actualArray.RemoveAll(string.IsNullOrWhiteSpace);
        expectedArray.RemoveAll(string.IsNullOrWhiteSpace);

        actualArray.Count.Should().Be(expectedArray.Count);
        for (var i = 0; i < actualArray.Count; i++)
        {
            if (i == 6) // Skip timestamp line
            {
                continue;
            }

            actualArray[i].Length.Should().Be(expectedArray[i].Length);
            actualArray[i].Should().Be(expectedArray[i]);
        }
    }

    private static async Task CheckTestOrders(IRequestDbContext requestDb)
    {
        var testOrderCount = await requestDb.TestOrders.CountAsync(TestContext.Current.CancellationToken);
        testOrderCount.Should().Be(6);
    }

    private static async Task CheckCurve(IResultDbContext resultDb)
    {
        var curve = await resultDb.Curves.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(curve);
        curve.Judgment.Should().Be(CurveJudgment.OK);
    }

    private static async Task CheckCurveControls(IResultDbContext resultDb)
    {
        var curveControl = await resultDb.CurveControlTestResults.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(curveControl);
        curveControl.JudgmentSummary.Should().Be(CurveControlJudgment.OK);
    }

    private static async Task CheckQualityControls(IResultDbContext resultDb)
    {
        var qualityControl = await resultDb.QualityControlTestResults.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(qualityControl);
        qualityControl.Judgment.Should().Be(QualityControlJudgement.Ok);
    }

    private static async Task CheckTestResults(IResultDbContext resultDb)
    {
        var c5 = await resultDb.TestResults.FirstOrDefaultAsync(r => r.TestName == "c5", TestContext.Current.CancellationToken);
        Assert.NotNull(c5);
        c5.MeanConcentrationStatus.Should().Be(ConcentrationStatus.Ok);

        var result = c5.MeanConcentrationAsText;
        Assert.NotNull(result);
        result = result.Replace(',', '.');
        result.Should().Be("2.53");
    }

    private static async Task InitParameters(IParametersDbContext db)
    {
        MethodConcentrationUnit concentrationUnit = new()
        {
            UnitId = 1,
            ConcentrationUnit = ""
        };
        MethodTest aIgE = new()
        {
            TestName = "a_IgE",
            DefaultLabTestName = "",
            DiluentIdentity = "",
            FullTestName = "",
            DefaultConcentrationUnit = concentrationUnit,
            ConcentrationFactor = 1.0
        };
        MethodTest d1 = new()
        {
            TestName = "d1",
            DefaultLabTestName = "",
            DiluentIdentity = "",
            FullTestName = "",
            DefaultConcentrationUnit = concentrationUnit,
            ConcentrationFactor = 1.0
        };

        var m = new Method
        {
            MethodName = "sIgE",
            FullMethodName = "sIgE",
            Calculation = new MethodCalculation
            {
                CalculationType = CalculationType.Rodbard4Par
            },
            Tests = new List<MethodTest>
            {
                aIgE,
                d1,
                new() { TestName = "t1", DefaultLabTestName = "", DiluentIdentity = "", FullTestName = "", DefaultConcentrationUnit = concentrationUnit, ConcentrationFactor = 1.0 },
                new() { TestName = "t2", DefaultLabTestName = "", DiluentIdentity = "", FullTestName = "", DefaultConcentrationUnit = concentrationUnit, ConcentrationFactor = 1.0 },
                new() { TestName = "t3", DefaultLabTestName = "", DiluentIdentity = "", FullTestName = "", DefaultConcentrationUnit = concentrationUnit, ConcentrationFactor = 1.0 },
                new() { TestName = "c1", DefaultLabTestName = "", DiluentIdentity = "", FullTestName = "", DefaultConcentrationUnit = concentrationUnit, ConcentrationFactor = 1.0 },
                new() { TestName = "c2", DefaultLabTestName = "", DiluentIdentity = "", FullTestName = "", DefaultConcentrationUnit = concentrationUnit, ConcentrationFactor = 1.0 },
                new() { TestName = "c5", DefaultLabTestName = "", DiluentIdentity = "", FullTestName = "", DefaultConcentrationUnit = concentrationUnit, ConcentrationFactor = 1.0, NumberOfDecimalsForConcentrationBelow10 = 2, NumberOfDecimalsForConcentrationBetween10And100 = 1, NumberOfDecimalsForConcentrationAbove100 = 0 }
            },
            CalibrationCurve = new MethodCalibrationCurve
            {
                MethodTest = aIgE,
                CalibrationPoints = new List<MethodCalibrationPoint>
                {
                    new() { CalibratorName = "CAL-0.0", ExpectedConcentration = 0.001 },
                    new() { CalibratorName = "CAL-0.35", ExpectedConcentration = 0.35 },
                    new() { CalibratorName = "CAL-0.70", ExpectedConcentration = 0.7 },
                    new() { CalibratorName = "CAL-3.50", ExpectedConcentration = 3.5 },
                    new() { CalibratorName = "CAL-17.5", ExpectedConcentration = 17.5 },
                    new() { CalibratorName = "CAL-100", ExpectedConcentration = 100 }
                }
            },
            CurveControls = new List<MethodCurveControl>
            {
                new() { CurveControlName = "CC-1", MethodTest = aIgE, ExpectedConcentration = 0.7, ExpectedStdDev = 0.07, InnerStdDevLimit = 2, OuterStdDevLimit = 3 },
                new() { CurveControlName = "CC-2", MethodTest = aIgE, ExpectedConcentration = 17.5, ExpectedStdDev = 1.75, InnerStdDevLimit = 2, OuterStdDevLimit = 3 }
            },
            AnalyticalRun = new MethodAnalyticalRun(),
            Presentation = new MethodPresentation
            {
                XAxisScale = new AxisScale(),
                YAxisScale = new AxisScale(),
                XAxisText = "",
                YAxisText = "",
                XAxisUnit = "",
                YAxisUnit = ""
            },
            Reagents = new List<MethodReagent>(),
            ClassTableBelows = new List<MethodClassTableBelow>(),
            ConcentrationUnits = new List<MethodConcentrationUnit>
            {
                concentrationUnit
            },
            ClassTables = new List<MethodClassTable>(),
            Reporting = new MethodReporting
            {
                DefaultTextQuantitativeAbove = "",
                DefaultTextQuantitativeBelow = ""
            }
        };

        await db.Methods.AddAsync(m);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    // ReSharper disable StringLiteralTypo
    private readonly string _allData =
        "SEP=;" + Environment.NewLine +
        "[HEADER]" + Environment.NewLine +
        "Date=2024-10-09" + Environment.NewLine +
        "Time=07:05:06" + Environment.NewLine +
        "TimeOffset=02:00:00" + Environment.NewLine +
        "FormatVersion=1.0.0" + Environment.NewLine +
        "FileName=C:\\ProgramData\\Thermo Fisher Scientific\\Instrument Simulator\\FLR\\FLR_SEED_001_2024-10-09_80ef333d-c02e-4388-aef7-80ccfe7eca39.log" + Environment.NewLine +
        "InstrumentModel=TetrisMedium2" + Environment.NewLine +
        "InstrumentSerial=SEED_001" + Environment.NewLine +
        "InstrumentSoftwareVersion=1.0.0" + Environment.NewLine +
        "AssayRunId=80ef333d-c02e-4388-aef7-80ccfe7eca39" + Environment.NewLine +
        "[ENTRIES]" + Environment.NewLine +
        "Date;Time;TestOrderSource;TestOrderId;GroupId;CmrTestId;ReplicateId;MethodName;Technology;SampleId;SampleBarcode;SampleActualBarcode;SampleSkipBarcodeRead;TubeId;SampleType;SampleTubeType;SampleGelHeight;SampleEstimatedLevel;SampleActualLevel;SampleAspiratedVol;SampleRemainingVol;SampleLoadDate;SampleLoadTime;SampleLoadInput;SampleLoadTrayID;SampleLoadPosition;SampleStoragePosition;SampleShuttle;SampleCartridgePosition;SampleCartridgeWell;SampleHighPriority;SamplePipAssayTime;SamplePipetteTipCore;SamplePipetteTipSample;SampleWashAssayTime;SampleOutput;SampleOutputTrayID;SampleOutputPosition;SamplePipettingStatus;TestName;TestBarcode;TestSkipBarcodeRead;TestStoragePos;TestStoragePosAfter;TestLoadDate;TestLoadTime;TestDispenseDate;TestDispenseTime;TestRemainingDoses;TestDispenseStatus;TestExpired;TestMoveERW;AnalyticalRunId;CustomRunId;DiluentName;DiluentBarcode;DiluentLoadDate;DiluentLoadTime;DiluentLocation;DiluentWell;DiluentPierceDate;DiluentPierceTime;DiluentExpired;DilutionPlateLoadDate;DilutionPlateLoadTime;DilutionStatus;ConjugateName;ConjugateBarcode;ConjugateLoadDate;ConjugateLoadTime;ConjugateLocation;ConjugateWell;ConjugatePierceDate;ConjugatePierceTime;ConjugatePipAssayTime;ConjugateLiquidLevel;ConjugateVolume;ConjugateRemainingVol;ConjugateExpired;ConjugatePipetteTip;ConjugateWash1AssayTime;ConjugateWash2AssayTime;ConjugateWash3AssayTime;DevelopmentName;DevelopmentBarcode;DevelopmentLoadDate;DevelopmentLoadTime;DevelopmentLocation;DevelopmentWell;DevelopmentPierceDate;DevelopmentPierceTime;DevelopmentPipAssayTime;DevelopmentLiquidLevel;DevelopmentVolume;DevelopmentRemainingVol;DevelopmentExpired;DevelopmentPipetteTip;DevelopmentStatus;StopName;StopBarcode;StopLoadDate;StopLoadTime;StopLocation;StopPip1AssayTime;StopPip2AssayTime;StopPip3AssayTime;StopVolume;StopLiquidLevel;StopRemainingVol;StopExpired;StopStatus;TransferAssayTime;TransferVolume;TransferStatus;WashConcName;WashConcBarcode;WashAddName;WashAddBarcode;PrewashVolume;SampleWashVolume;ConjugateWash1Volume;ConjugateWash2Volume;ConjugateWash3Volume;PrewashMixDate;PrewashMixTime;SampleWashMixDate;SampleWashMixTime;ConjugateWash1MixDate;ConjugateWash1MixTime;ConjugateWash2MixDate;ConjugateWash2MixTime;ConjugateWash3MixDate;ConjugateWash3MixTime;SamplePipetteWashVol;ConjugatePipetteWashVol;DevelopmentPipetteWashVol;MeasurementType;MeasurementValue;Measurement01;Measurement02;Measurement03;Measurement04;Measurement05;Measurement06;Measurement07;Measurement08;Measurement09;Measurement10;Measurement11;MeasurementDate;MeasurementTime;MeasurementAssayTime;FluorometerCurrent;FluorometerRef;FluorometerTemp;FluorometerVolume;FluorometerStatus;CarrierStorageAverageTemp;CarrierStorageDeviationTemp;SampleAverageTemp;SampleDeviationTemp;DiluentAverageTemp;DiluentDeviationTemp;ConjugateAverageTemp;ConjugateDeviationTemp;DevelopmentAverageTemp;DevelopmentDeviationTemp;RoomAverageTemp;RoomDeltaTemp;RoomDeviationTemp;ERWAverageTemp;IRWAverageTemp;PreDilution;InstrumentDilution;DilutionPlateId;DilutionPlateBarcode;Dilstep1DiluentLevel;Dilstep1DiluentVol;Dilstep1SampleVol;Dilstep1PlatePos;Dilstep1SampleLevel;Dilstep1RemainingVol;Dilstep1Replicate;" + Environment.NewLine +
        "2024-10-09;07:05:06;-;-;-;-;-;-;1;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;-;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;4;138.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:06;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:06;-;-;-;-;-;-;2;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;-;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;4;138.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:06;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:07;-;-;91f40f1d-a848-4580-80cc-898c1bb77d16;;f3845fc2-6b02-4ca5-abf6-c244a42cd24e;sIgE;-;CAL-0.00;A5SXXXSS0001;-;-;-;4;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;1592.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:07;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:07;-;-;91f40f1d-a848-4580-80cc-898c1bb77d16;;3fb24d3e-482d-4d71-8a32-28c22c88fadf;sIgE;-;CAL-0.00;A5SXXXSS0001;-;-;-;4;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;1598.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:07;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:07;-;-;91f40f1d-a848-4580-80cc-898c1bb77d16;;94e35bb0-443d-464b-ab18-eefa7559b646;sIgE;-;CAL-0.35;A5SXXXSS0001;-;-;-;4;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;8833.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:07;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:07;-;-;91f40f1d-a848-4580-80cc-898c1bb77d16;;2ffef76f-e7f5-47bb-b1b0-f839462f797a;sIgE;-;CAL-0.35;A5SXXXSS0001;-;-;-;4;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;8831.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:07;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:07;-;-;91f40f1d-a848-4580-80cc-898c1bb77d16;;18fa9a29-bb87-4b9d-a665-8a5131e1350b;sIgE;-;CAL-0.70;A5SXXXSS0001;-;-;-;4;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;16630.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:07;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:07;-;-;91f40f1d-a848-4580-80cc-898c1bb77d16;;15240546-2e1e-44c6-b4e0-f59b8b0b3ae7;sIgE;-;CAL-0.70;A5SXXXSS0001;-;-;-;4;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;16631.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:07;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:07;-;-;91f40f1d-a848-4580-80cc-898c1bb77d16;;d2ac77e2-a6d6-4aef-887c-beba0f1c1891;sIgE;-;CAL-3.50;A5SXXXSS0001;-;-;-;4;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;73591.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:07;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:07;-;-;91f40f1d-a848-4580-80cc-898c1bb77d16;;09c06bda-fb44-4c7d-af55-289d5cf2c16b;sIgE;-;CAL-3.50;A5SXXXSS0001;-;-;-;4;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;73593.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:07;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:07;-;-;91f40f1d-a848-4580-80cc-898c1bb77d16;;35427311-930e-4828-aa18-7e260e73405d;sIgE;-;CAL-17.5;A5SXXXSS0001;-;-;-;4;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;308794.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:07;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:07;-;-;91f40f1d-a848-4580-80cc-898c1bb77d16;;e5b0df3d-2fe7-4685-bb49-09b2085d38f0;sIgE;-;CAL-17.5;A5SXXXSS0001;-;-;-;4;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;308798.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:07;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:07;-;-;91f40f1d-a848-4580-80cc-898c1bb77d16;;cf526a63-9090-450b-8597-3d6a9a2e2eaa;sIgE;-;CAL-100;A5SXXXSS0001;-;-;-;4;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;824473.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:07;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:07;-;-;91f40f1d-a848-4580-80cc-898c1bb77d16;;65e39090-c01c-4220-856f-704bf96824c7;sIgE;-;CAL-100;A5SXXXSS0001;-;-;-;4;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;824471.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:07;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:08;-;-;86c3167b-4a33-44f3-a900-d67ad7f1a65a;;68588234-a3d4-4c13-9321-4c5479eba52a;sIgE;-;CC-1;A0VXXXSS0001;-;-;-;3;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;16637.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:08;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:08;-;-;86c3167b-4a33-44f3-a900-d67ad7f1a65a;;de95c413-1ece-40ae-a0bc-9ec50a489192;sIgE;-;CC-2;A0VXXXSS0001;-;-;-;3;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;a_IgE;818XXSS;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;308797.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:08;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:09;-;-;46f1eeb2-4d8b-4fc0-a47c-5af72d1ca4f2;;e3e9e4ab-ade9-413a-8a49-36115d7b3065;sIgE;-;sIgE Pos;AVMXXXSS0001;-;-;-;1;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;d1;088XX01;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;40096.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:09;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:11;-;-;580f869d-a21b-4d7a-aedb-928ca3665679;;d6e464e0-eb5e-4c4a-9388-bd4d5b03e887;sIgE;-;BC-KBQWQ1;BC-KBQWQ1;-;-;-;0;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;t1;409XXGL;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;67422.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:11;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:12;-;-;f2f9f828-f80a-41bd-9186-1863eed5f547;;a96e46fd-41d4-43e3-ab5c-f288d3ade465;sIgE;-;BC-KBQWQ1;BC-KBQWQ1;-;-;-;0;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;t2;409XXGL;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;51390.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:12;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:12;-;-;a428b242-cf53-41d5-b60a-286bc4cc1cd3;;a72297c6-e829-4e90-b5b2-5d179a692560;sIgE;-;BC-KBQWQ1;BC-KBQWQ1;-;-;-;0;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;t3;409XXGL;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;57607.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:12;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:12;-;-;dc4e4379-66d4-4af1-af1e-d34378e1f740;;8c357b4c-f7d6-48a2-9a88-1a9c38c9f219;sIgE;-;BC-KBQWQ1;BC-KBQWQ1;-;-;-;0;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;c1;409XXGL;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;64591.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:12;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:12;-;-;3e6010ff-cd8d-4e6b-a9d0-50faa1e38e70;;d03a6602-81d9-4499-87ff-485b576b282d;sIgE;-;BC-KBQWQ1;BC-KBQWQ1;-;-;-;0;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;c2;409XXGL;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;69891.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:12;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine +
        "2024-10-09;07:05:12;-;-;44a3f415-591f-43ad-8536-153971bf0f8b;;965098e6-65b2-4260-93c9-bb0385546516;sIgE;-;BC-KBQWQ1;BC-KBQWQ1;-;-;-;0;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;c5;409XXGL;-;-;-;-;-;-;-;-;-;False;-;267d4ee0-072f-4697-9d72-95afd85e7a6f;;-;;-;-;-;;-;-;;-;-;-;-;A3TXXXEM0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;-;-;C4KXXXNZ0001;-;-;-;1;-;-;-;-;-;-;False;-;-;-;CKSXXXFA0001;-;-;-;-;-;-;-;-;-;False;-;-;-;-;-;;-;;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;0;54025.0000;-;-;-;-;-;-;-;-;-;-;-;2024-10-09;07:05:12;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;-;1;1;-;;-;-;-;-;-;-;-;" + Environment.NewLine;

    private readonly string _minimumData =
        "[HEADER];;;;;;;;;;;;" + Environment.NewLine +
        "Date=2024-10-09;;;;;;;;;;;;" + Environment.NewLine +
        "Time=07:05:06;;;;;;;;;;;;" + Environment.NewLine +
        "TimeOffset=02:00:00;;;;;;;;;;;;" + Environment.NewLine +
        "FormatVersion=1.0.0;;;;;;;;;;;;" + Environment.NewLine +
        "FileName=C:\\ProgramData\\Thermo Fisher Scientific\\Instrument Simulator\\FLR\\FLR_SEED_001_2024-10-09_80ef333d-c02e-4388-aef7-80ccfe7eca39.log;;;;;;;;;;;;" + Environment.NewLine +
        "InstrumentModel=TetrisMedium;;;;;;;;;;;;" + Environment.NewLine +
        "InstrumentSerial=SEED_001;;;;;;;;;;;;" + Environment.NewLine +
        "InstrumentSoftwareVersion=1.0.0;;;;;;;;;;;;" + Environment.NewLine +
        "AssayRunId=60ef333d-c02e-4388-aef7-80ccfe7eca39;;;;;;;;;;;;" + Environment.NewLine +
        ";;;;;;;;;;;;" + Environment.NewLine +
        "[ENTRIES];;;;;;;;;;;;" + Environment.NewLine +
        "Date;Time;MethodName;Technology;SampleId;SampleType;TestName;MeasurementType;MeasurementValue;MeasurementDate;MeasurementTime;PreDilution;InstrumentDilution" + Environment.NewLine +
        "2024-10-09;07:05:06;-;1;-;-;-;4;138.0000;2024-10-09;07:05:06;-;-" + Environment.NewLine +
        "2024-10-09;07:05:06;-;2;-;-;-;4;138.0000;2024-10-09;07:05:06;-;-" + Environment.NewLine +
        "2024-10-09;07:05:07;sIgE;-;CAL-0.00;4;a_IgE;0;1592.0000;2024-10-09;07:05:07;1;1" + Environment.NewLine +
        "2024-10-09;07:05:07;sIgE;-;CAL-0.00;4;a_IgE;0;1598.0000;2024-10-09;07:05:07;1;1" + Environment.NewLine +
        "2024-10-09;07:05:07;sIgE;-;CAL-0.35;4;a_IgE;0;8833.0000;2024-10-09;07:05:07;1;1" + Environment.NewLine +
        "2024-10-09;07:05:07;sIgE;-;CAL-0.35;4;a_IgE;0;8831.0000;2024-10-09;07:05:07;1;1" + Environment.NewLine +
        "2024-10-09;07:05:07;sIgE;-;CAL-0.70;4;a_IgE;0;16630.0000;2024-10-09;07:05:07;1;1" + Environment.NewLine +
        "2024-10-09;07:05:07;sIgE;-;CAL-0.70;4;a_IgE;0;16631.0000;2024-10-09;07:05:07;1;1" + Environment.NewLine +
        "2024-10-09;07:05:07;sIgE;-;CAL-3.50;4;a_IgE;0;73591.0000;2024-10-09;07:05:07;1;1" + Environment.NewLine +
        "2024-10-09;07:05:07;sIgE;-;CAL-3.50;4;a_IgE;0;73593.0000;2024-10-09;07:05:07;1;1" + Environment.NewLine +
        "2024-10-09;07:05:07;sIgE;-;CAL-17.5;4;a_IgE;0;308794.0000;2024-10-09;07:05:07;1;1" + Environment.NewLine +
        "2024-10-09;07:05:07;sIgE;-;CAL-17.5;4;a_IgE;0;308798.0000;2024-10-09;07:05:07;1;1" + Environment.NewLine +
        "2024-10-09;07:05:07;sIgE;-;CAL-100;4;a_IgE;0;824473.0000;2024-10-09;07:05:07;1;1" + Environment.NewLine +
        "2024-10-09;07:05:07;sIgE;-;CAL-100;4;a_IgE;0;824471.0000;2024-10-09;07:05:07;1;1" + Environment.NewLine +
        "2024-10-09;07:05:08;sIgE;-;CC-1;3;a_IgE;0;16637.0000;2024-10-09;07:05:08;1;1" + Environment.NewLine +
        "2024-10-09;07:05:08;sIgE;-;CC-2;3;a_IgE;0;308797.0000;2024-10-09;07:05:08;1;1" + Environment.NewLine +
        "2024-10-09;07:05:09;sIgE;-;sIgE Pos;1;d1;0;40096.0000;2024-10-09;07:05:09;1;1" + Environment.NewLine +
        "2024-10-09;07:05:11;sIgE;-;BC-KBQWQ1;0;t1;0;67422.0000;2024-10-09;07:05:11;1;1" + Environment.NewLine +
        "2024-10-09;07:05:12;sIgE;-;BC-KBQWQ1;0;t2;0;51390.0000;2024-10-09;07:05:12;1;1" + Environment.NewLine +
        "2024-10-09;07:05:12;sIgE;-;BC-KBQWQ1;0;t3;0;57607.0000;2024-10-09;07:05:12;1;1" + Environment.NewLine +
        "2024-10-09;07:05:12;sIgE;-;BC-KBQWQ1;0;c1;0;64591.0000;2024-10-09;07:05:12;1;1" + Environment.NewLine +
        "2024-10-09;07:05:12;sIgE;-;BC-KBQWQ1;0;c2;0;69891.0000;2024-10-09;07:05:12;1;1" + Environment.NewLine +
        "2024-10-09;07:05:12;sIgE;-;BC-KBQWQ1;0;c5;0;54025.0000;2024-10-09;07:05:12;1;1" + Environment.NewLine;
}