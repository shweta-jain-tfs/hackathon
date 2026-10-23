using MediatR;
using Microsoft.Extensions.Logging;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Result.Application.Events.TestResultReady;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

public class CalibratorResultGroupReadyEventHandlerTests
{
    [Fact]
    public async Task Handle_FullyAbortedLevel_ShouldCheckCurveUsingRemainingResponses()
    {
        const string instrumentNumber = "SN001";
        const string methodName = "Method1";
        var groupId = Guid.NewGuid();

        var abortedResult = CreateResult(groupId, response: null, isAborted: true);
        var validResult = CreateResult(groupId, response: 100, isAborted: false);
        var results = new List<CalibratorTestResult> { abortedResult, validResult };

        var dbContextMock = new Mock<IResultDbContext>();
        dbContextMock.Setup(db => db.CalibratorTestResults).ReturnsDbSet(results);
        dbContextMock.Setup(db => db.TestReplicateErrors).ReturnsDbSet(new List<TestReplicateError>());
        dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var methodsServiceMock = new Mock<IMethodsService>();
        methodsServiceMock
            .Setup(service => service.GetMethod(methodName, It.IsAny<System.Linq.Expressions.Expression<Func<Method, int>>>()))
            .ReturnsAsync(results.Count);

        var calculationServiceMock = new Mock<IResultCalculationService>();
        calculationServiceMock
            .Setup(service => service.CheckAndFitCurve(
                instrumentNumber,
                groupId,
                It.IsAny<List<CalibratorTestResult>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Curve());

        var sut = new CalibratorResultGroupReadyEventEventHandler(
            dbContextMock.Object,
            calculationServiceMock.Object,
            methodsServiceMock.Object,
            Mock.Of<ISender>(),
            Mock.Of<ILogger<CalibratorResultGroupReadyEventEventHandler>>());

        await sut.Handle(
            new CalibratorResultGroupReadyEvent(instrumentNumber, groupId, methodName),
            TestContext.Current.CancellationToken);

        calculationServiceMock.Verify(service => service.CheckAndFitCurve(
            instrumentNumber,
            groupId,
            It.Is<List<CalibratorTestResult>>(items => items.SequenceEqual(results)),
            It.IsAny<CancellationToken>()), Times.Once);
        abortedResult.Status.Should().Be(TestResultStatus.Aborted);
        validResult.Status.Should().Be(TestResultStatus.Ready);
    }

    private static CalibratorTestResult CreateResult(Guid groupId, double? response, bool isAborted)
    {
        return new CalibratorTestResult
        {
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            GroupId = groupId,
            GroupSize = 2,
            NumberOfReplicates = 1,
            Replicates =
            [
                new CalibratorTestReplicate
                {
                    Active = true,
                    Response = response,
                    IsAborted = isAborted
                }
            ]
        };
    }
}
