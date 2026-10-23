using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;
using Pharos.Server.Result.Application.Commands;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class ApproveTestResultsCommandTests : IntegrationTestBase
{
    private readonly Fixture _fixture;

    public ApproveTestResultsCommandTests(IntegrationTestsWaf factory) : base(factory)
    {
        _fixture = new Fixture();
    }

    [Fact]
    public async Task ApproveTestResultsCommand_Should_SendTestResultLisMessageToLis_WithCorrectSampleId()
    {
        // Arrange
        Factory.LisConnectionMock?.Invocations.Clear();

        var sampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength];

        var testOrderUid = Guid.NewGuid();
        var sampleTestResult = new SampleTestResult
        {
            Status = TestResultStatus.PendingApproval,
            InstrumentNumber = "SN1",
            TestName = "f1",
            MethodName = "sIgE",
            MeanConcentrationAsText = "1.5",
            MeanClass = "Negative",
            RequestUid =
            {
                SampleId = sampleId,
                RequestCreatedDate = DateTimeOffset.Now
            },
            TestOrderId = testOrderUid,
            AnalyticalRun = new AnalyticalRun
            {
                Id = 0,
                InstrumentAnalyticalRunId = Guid.NewGuid(),
                MethodName = "A",
                InstrumentNumber = "B",
            }
        };

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<IRequestDbContext>();
            appDbContext.TestOrders.Add(new TestOrder
            {
                Uid = testOrderUid,
                LisChannelId = Factory.LisConnectionMock?.Object.LisChannelId,
                TestName = "f1",
                MethodName = "sIgE",
                Request = new Request.Domain.Request
                {
                    SampleId = sampleId,
                    Source = RequestSource.Lis,
                }
            }
            );

            await appDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var resultDbContext = scope.ServiceProvider.GetRequiredService<IResultDbContext>();
            resultDbContext.SampleTestResults.Add(sampleTestResult);
            await resultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using (var asyncScope = Factory.Services.CreateAsyncScope())
        {
            var sender = asyncScope.ServiceProvider.GetRequiredService<ISender>();
            var command = new ApproveTestResultsCommand([sampleTestResult.TestOrderId], false);

            await sender.Send(command, TestContext.Current.CancellationToken);
        }

        // Wait for background notifications to complete
        await WaitForNotificationsAsync();

        // Wait for asynchronous LIS dispatch to complete
        await WaitForHubResponse(() => Factory.LisConnectionMock?.Invocations.Any(i => i.Method.Name == nameof(ILisConnection.SendMessageAsync)) == true);

        // Assert
        Factory.LisConnectionMock?.Verify(x => x.SendMessageAsync(LisMessageType.SampleTestResult, It.Is<string>(s => s.Contains(sampleId))), Times.Once);
    }

    [Fact]
    public async Task ApproveTestResultsCommand_Should_ThrowWhenMixedFlaggedSelectionIsApproved()
    {
        // Arrange
        var flaggedSampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength];
        var approvedSampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength];

        var flaggedTestOrderUid = Guid.NewGuid();
        var approvedTestOrderUid = Guid.NewGuid();

        var flaggedResult = new SampleTestResult
        {
            Status = TestResultStatus.Flagged,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            InstrumentNumber = "SN1",
            TestName = "f1",
            MethodName = "sIgE",
            MeanConcentrationAsText = "1.5",
            MeanClass = "Negative",
            RequestUid =
            {
                SampleId = flaggedSampleId,
                RequestCreatedDate = DateTimeOffset.Now
            },
            TestOrderId = flaggedTestOrderUid,
            AnalyticalRun = new AnalyticalRun
            {
                InstrumentAnalyticalRunId = Guid.NewGuid(),
                MethodName = "A",
                InstrumentNumber = "B",
            }
        };

        var pendingResult = new SampleTestResult
        {
            Status = TestResultStatus.PendingApproval,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            InstrumentNumber = "SN1",
            TestName = "f2",
            MethodName = "sIgE",
            MeanConcentrationAsText = "2.5",
            MeanClass = "Positive",
            RequestUid =
            {
                SampleId = approvedSampleId,
                RequestCreatedDate = DateTimeOffset.Now
            },
            TestOrderId = approvedTestOrderUid,
            AnalyticalRun = new AnalyticalRun
            {
                InstrumentAnalyticalRunId = Guid.NewGuid(),
                MethodName = "A",
                InstrumentNumber = "B",
            }
        };

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var appDbContext = scope.ServiceProvider.GetRequiredService<IRequestDbContext>();
            appDbContext.TestOrders.AddRange(
                new TestOrder
                {
                    Uid = flaggedTestOrderUid,
                    TestName = "f1",
                    MethodName = "sIgE",
                    Request = new Request.Domain.Request
                    {
                        SampleId = flaggedSampleId,
                        Source = RequestSource.Lis,
                    }
                },
                new TestOrder
                {
                    Uid = approvedTestOrderUid,
                    TestName = "f2",
                    MethodName = "sIgE",
                    Request = new Request.Domain.Request
                    {
                        SampleId = approvedSampleId,
                        Source = RequestSource.Lis,
                    }
                });

            await appDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var resultDbContext = scope.ServiceProvider.GetRequiredService<IResultDbContext>();
            resultDbContext.SampleTestResults.AddRange(flaggedResult, pendingResult);
            await resultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        await using var asyncScope = Factory.Services.CreateAsyncScope();
        var sender = asyncScope.ServiceProvider.GetRequiredService<ISender>();

        var act = async () => await sender.Send(new ApproveTestResultsCommand([flaggedTestOrderUid, approvedTestOrderUid], false), TestContext.Current.CancellationToken);

        // Assert
        var exception = await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(act);
        Assert.Contains("flagged result cannot be approved together with other selected results", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
