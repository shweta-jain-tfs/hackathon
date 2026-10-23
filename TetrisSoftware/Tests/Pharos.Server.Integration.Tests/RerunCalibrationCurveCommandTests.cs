using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Result.Application.Commands;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class RerunCalibrationCurveCommandTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ReRunCalibratorCurve_Should_ResetConcentrationForNonApprovedTest() //Rerun should reset calculations 
    {
        // Arrange
        var (_, instrumentState) = await Factory.AddInstrumentConnection();
        var serialNumber = instrumentState.SerialNumber;
        MockInstrumentInAssayMode(serialNumber);

        await using (var asyncScope = Factory.Services.CreateAsyncScope())
        {
            var mediator = asyncScope.ServiceProvider.GetService<IMediator>()!;
            var dbContext = asyncScope.ServiceProvider.GetRequiredService<IResultDbContext>();
            await AddAnalyticalRun(serialNumber, dbContext);
            await AddSampleTest(serialNumber, ResultApprovalStatus.NotApproved, TestResultStatus.PendingApproval, dbContext);

            dbContext.SampleTestResults.Count().Should().Be(1);
            dbContext.SampleTestResults.First().MeanConcentration.Should().Be(1);

            var command = RerunCalibratorCurveCommand();

            // Act
            await mediator.Send(command, TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        // Assert
        var dbContext1 = ServiceProvider.GetRequiredService<IResultDbContext>();
        var result = await dbContext1.SampleTestResults
            .AsQueryable()
            .Include(x => x.Replicates)
            .FirstAsync(TestContext.Current.CancellationToken);

        result.MeanConcentration.Should().Be(null);
        result.MeanConcentrationStatus.Should().Be(ConcentrationStatus.NotAvailable);
        result.MeanCutOff.Should().Be(null);
        result.MeanConcentrationAsText.Should().Be(null);
        result.MeanResponse.Should().Be(1);
        result.Replicates!.First().Concentration.Should().Be(null);
        result.Replicates!.First().Response.Should().Be(1);

        dbContext1.AnalyticalRuns.First().CalibrationCurveIssue.Should().Be(AnalyticalRunIssue.NoIssue);
        dbContext1.AnalyticalRuns.First().CurveState.Should().Be(CurveState.WaitingForNewCurve);
    }

    [Fact]
    public async Task ReRunCalibratorCurve_ShouldNot_ResetConcentrationForRejectedTest()
    {
        //Arrange
        var (_, instrumentState) = await Factory.AddInstrumentConnection();
        var serialNumber = instrumentState.SerialNumber;
        MockInstrumentInAssayMode(serialNumber);

        await using (var asyncScope = Factory.Services.CreateAsyncScope())
        {
            var mediator = asyncScope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = asyncScope.ServiceProvider.GetRequiredService<IResultDbContext>();

            await AddAnalyticalRun(serialNumber, dbContext);
            await AddSampleTest(serialNumber, ResultApprovalStatus.Rejected, TestResultStatus.Rejected, dbContext);

            dbContext.SampleTestResults.Count().Should().Be(1);
            dbContext.SampleTestResults.First().MeanConcentration.Should().Be(1);

            var command = RerunCalibratorCurveCommand();

            //Act
            await mediator.Send(command, TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        //Assert
        var dbContext1 = ServiceProvider.GetRequiredService<IResultDbContext>();
        var result = await dbContext1.SampleTestResults
            .AsQueryable()
            .Include(x => x.Replicates)
            .FirstAsync(TestContext.Current.CancellationToken);
        result.MeanConcentration.Should().Be(1);
        result.MeanCutOff.Should().Be("Positive");
        result.MeanConcentrationAsText.Should().Be("1.0");
        result.MeanConcentrationStatus.Should().Be(ConcentrationStatus.Ok);
        result.MeanResponse.Should().Be(1);
        result.Replicates!.First().Concentration.Should().Be(1);
        result.Replicates!.First().Response.Should().Be(1);

        (await dbContext1.AnalyticalRuns.FirstAsync(TestContext.Current.CancellationToken)).CalibrationCurveIssue.Should().Be(AnalyticalRunIssue.NoIssue);
    }

    private void MockInstrumentInAssayMode(string serialNumber)
    {
        //TODO: Update to check that the curve is run in the current assay, not just any assay.
        var instrumentManager = ServiceProvider.GetService<IInstrumentManager>()!;
        var instrument = (Instrument.Domain.Instrument)instrumentManager.GetInstrument(serialNumber);
        instrument.State = InstrumentState.Assay;
    }

    private static RerunCalibratorCurveCommand RerunCalibratorCurveCommand()
    {
        return new RerunCalibratorCurveCommand(1);
    }

    private static async Task AddSampleTest(string serialNumber, ResultApprovalStatus approvalStatus, TestResultStatus resultStatus, IResultDbContext dbContext)
    {
        var sampleTestResult = new SampleTestResult
        {
            AnalyticalRunId = 1,
            Status = resultStatus,
            ApprovalStatus = approvalStatus,
            InstrumentNumber = serialNumber,
            TestName = "c1",
            MethodName = "sIgE",
            MeanConcentrationAsText = "1.0",
            MeanConcentration = 1,
            MeanConcentrationStatus = ConcentrationStatus.Ok,
            MeanResponse = 1,
            MeanCutOff = "Positive",
            RequestUid =
            {
                SampleId = "Sample123",
                RequestCreatedDate = DateTimeOffset.Now
            },
            TestOrderId = Guid.NewGuid(),
            Replicates = new List<TestReplicate>
            {
                new SampleTestReplicate
                {
                    Concentration = 1,
                    Response = 1
                }
            }
        };

        dbContext.SampleTestResults.Add(sampleTestResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task AddAnalyticalRun(string serialNumber, IResultDbContext dbContext)
    {
        var ar = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = serialNumber,
            CalibrationCurveIssue = AnalyticalRunIssue.Warning,
            ConjugateLotNumber = "conjL"
        };

        dbContext.AnalyticalRuns.Add(ar);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
