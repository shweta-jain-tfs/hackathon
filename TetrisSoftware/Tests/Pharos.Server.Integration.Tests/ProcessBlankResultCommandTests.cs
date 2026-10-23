using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;


namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class ProcessBlankResultCommandTests : IntegrationTestBase
{
    public ProcessBlankResultCommandTests(IntegrationTestsWaf factory) : base(factory) { }

    [Theory]
    [InlineData(true, "123456789012", "", 1, "SN1", EnumBlankType.ReagentImmunoCap)] //Matching instrument id, cell position and development solution barcode
    [InlineData(false, "123456789012", "", 1, "SN2", EnumBlankType.ReagentImmunoCap)] //Different instrument id
    [InlineData(false, "123456789012", "", 2, "SN1", EnumBlankType.ReagentImmunoCap)] //Different cell position 
    [InlineData(false, "923456789012", "", 1, "SN1", EnumBlankType.ReagentImmunoCap)] //Different development solution barcode
    [InlineData(false, "123456789012", "", 1, "SN1", EnumBlankType.ReagentElia)] //Wrong technology
    [Trait("ModuleRequirement", "MR_3500")]
    public async Task ReagentBlankReceived(bool shouldCalculateResponse, string devSolBarcode, string devSolUdi, int cellPos, string instrumentNumber, EnumBlankType blankType)
    {
        //Arrange
        await using (var scope = Factory.Services.CreateAsyncScope())
        {

            var dbContext = scope.ServiceProvider.GetService<IResultDbContext>()!;
            var methodContext = scope.ServiceProvider.GetService<IParametersDbContext>()!;
            var mediator = scope.ServiceProvider.GetService<IMediator>()!;
            const string devSolutionBarcode = "123456789012";
            const int cellPosition = 1;

            await Seed(dbContext, methodContext, devSolBarcode, devSolUdi, cellPos, instrumentNumber);

            var resultBeforeReceivingReagentBlank = await dbContext.TestResults.SingleOrDefaultAsync(TestContext.Current.CancellationToken);
            resultBeforeReceivingReagentBlank.Should().NotBeNull();
            resultBeforeReceivingReagentBlank.MeanResponse.Should().BeNull();

            var blankResult = new BlankResult
            {
                BlankType = blankType,
                ResultOk = true,
                MeanValue = 10,
                UpperLimit = 9999,
                LowerLimit = 0,
                DevelopmentSolutionIdentity = new DevelopmentSolutionIdentity
                {
                    ReagentBarcode = devSolutionBarcode,
                    CellPosition = cellPosition
                },
                StopSolutionBarcode = "StopSolutionBarcode"
            };

            var notification = new ProcessBlankResultCommand("SN1", blankResult);
            await mediator.Send(notification, TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        // Assert 
        var dbContext1 = ServiceProvider.GetService<IResultDbContext>()!;
        var resultAfter = await dbContext1.TestResults.SingleOrDefaultAsync(TestContext.Current.CancellationToken);

        resultAfter.Should().NotBeNull();

        if (shouldCalculateResponse)
        {
            resultAfter.MeanResponse.Should().NotBeNull();
        }
        else
        {
            resultAfter.MeanResponse.Should().BeNull();
        }
    }

    private static async Task Seed(IResultDbContext dbContext, IParametersDbContext methodContext, string devSolBarcode, string devSolUdi, int cellPos, string instrumentNumber)
    {
        // Seed Sample test result
        var analyticalRun = new AnalyticalRun
        {
            MethodName = "A",
            InstrumentNumber = "B"
        };
        dbContext.AnalyticalRuns.Add(analyticalRun);

        var testResult = new SampleTestResult
        {
            RequestUid =
            {
                SampleId = "SID123"
            },
            MethodName = "sIgE",
            TestName = "c1",
            NumberOfReplicates = 1,
            InstrumentDilutionFactor = 1,
            PreDilutionFactor = 1,
            InstrumentNumber = instrumentNumber,
            Replicates = new List<TestReplicate>
            {
                new SampleTestReplicate
                {
                    FluorometerValue = 400000,
                    TestReplicateReagents = new List<TestReplicateReagent>
                    {
                        new()
                        {
                            ReagentType = ArticleType.DevelopmentSolutionCartridge,
                            Barcode = devSolBarcode,
                            Udi = devSolUdi,
                            SerialNumber = devSolBarcode.Substring(8, 4),
                            CellPosition = cellPos,
                            LotNumber = "123"
                        }
                    }
                }
            },
            AnalyticalRun = analyticalRun
        };

        dbContext.SampleTestResults.Add(testResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fixture = new Fixture();
        var method = new Method
        {
            MethodName = "sIgE",
            Technology = AssayTechnology.ImmunoCap,
            FullMethodName = "ImmunoCap Specific IgE 0-100",
            MethodNameLis = "sIgE",
            Enabled = true,
            AnalyticalRun = new MethodAnalyticalRun(),
            Calculation = new MethodCalculation(),
            Presentation = fixture.Create<MethodPresentation>(),
            Reporting = fixture.Create<MethodReporting>(),
            Reagents =
            [
                new MethodReagent
                {
                    ReagentType = ReagentType.Conjugate,
                    DefaultReagentIdentity = "sIgE"
                }
            ]
        };

        methodContext.Methods.Add(method);
        await methodContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}