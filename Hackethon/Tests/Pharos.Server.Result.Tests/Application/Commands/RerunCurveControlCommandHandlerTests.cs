using MediatR;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using TestResult = Pharos.Server.Result.Domain.TestResult;

namespace Pharos.Server.Result.Application.Commands;

public class RerunCurveControlCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCurveSourceIsCurrentAssay_ShouldNotSetWaitingForCurveControlOrOrderCc()
    {
        // Arrange
        var analyticalRun = new AnalyticalRun
        {
            Id = 1,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            ConjugateLotNumber = "Lot1",
            CarrierLotNumber = "Carrier1",
            CurveSource = CurveSource.CurveFromCurrentAssay,
            CurveState = CurveState.Invalid,
            TestResults = new List<TestResult>()
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(db => db.AnalyticalRuns).ReturnsDbSet(new List<AnalyticalRun> { analyticalRun });

        var mediatorMock = new Mock<IMediator>();
        var instrumentManagerMock = new Mock<IInstrumentManager>();
        var instrumentMock = new Mock<IInstrument>();
        instrumentMock.SetupGet(instrument => instrument.IsInAssayMode).Returns(true);
        instrumentManagerMock
            .Setup(manager => manager.FindInstrument("SN001"))
            .Returns(instrumentMock.Object);

        var sut = new RerunCurveControlCommandHandler(mediatorMock.Object, dbMock.Object, instrumentManagerMock.Object);

        // Act
        await sut.Handle(new RerunCurveControlCommand { AnalyticalRunId = 1 }, TestContext.Current.CancellationToken);

        // Assert
        analyticalRun.CurveState.Should().Be(CurveState.Invalid);
        analyticalRun.CurveControlTotalJudgment.Should().Be(CurveControlJudgmentResult.NotJudged);
        mediatorMock.Verify(
            m => m.Send(It.IsAny<OrderNewCalCcCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
