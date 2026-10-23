using MediatR;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Instrument.Application.Commands.UpdateMethods;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;
using Tetris.Protocol.Instrument.Methods;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Instrument.Application.Commands;

public class UpdateMethodsCommandHandlerTests
{
    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_ShouldSendOnlyMethodsConfiguredForInstrument()
    {
        // Arrange
        var instrument = new Domain.Instrument
        {
            Id = 1,
            SerialNumber = "SEED_001"
        };
        var dbContextMock = new Mock<IInstrumentDbContext>();
        dbContextMock.Setup(x => x.InstrumentMethods).ReturnsDbSet(
        [
            new InstrumentMethod
            {
                InstrumentId = instrument.Id,
                Instrument = instrument,
                MethodName = "Selected"
            }
        ]);
        var methodsServiceMock = new Mock<IMethodsService>();
        methodsServiceMock.Setup(x => x.GetDetailedMethods()).ReturnsAsync(
        [
            CreateMethod("Selected"),
            CreateMethod("Unselected")
        ]);
        var connectionMock = new Mock<IInstrumentConnection>();
        Methods? sentMethods = null;
        connectionMock
            .Setup(x => x.UpdateMethodsAsync(It.IsAny<Methods>(), It.IsAny<CancellationToken>()))
            .Callback<Methods, CancellationToken>((methods, _) => sentMethods = methods)
            .Returns(Task.CompletedTask);
        var session = new CommunicationSession
        {
            SessionId = Guid.NewGuid(),
            InstrumentNumber = instrument.SerialNumber,
            Connection = connectionMock.Object
        };
        var sessionManagerMock = new Mock<ICommunicationSessionManager>();
        sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);
        var publisherMock = new Mock<IPublisher>();
        publisherMock.Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = new UpdateMethodsCommandHandler(
            sessionManagerMock.Object,
            methodsServiceMock.Object,
            dbContextMock.Object,
            publisherMock.Object);

        // Act
        await sut.Handle(new UpdateMethodsCommand(instrument.SerialNumber), CancellationToken.None);

        // Assert
        sentMethods.Should().NotBeNull();
        sentMethods.Methods_.Select(x => x.MethodName).Should().Equal("Selected");
    }

    private static Method CreateMethod(string methodName) =>
        new()
        {
            MethodName = methodName,
            FullMethodName = methodName,
            AnalyticalRun = new MethodAnalyticalRun(),
            CalibrationCurve = new MethodCalibrationCurve
            {
                CalibrationPoints = [],
                MethodTest = new MethodTest
                {
                    TestName = "Calibrator"
                }
            },
            CurveControls = [],
            Reagents = [],
            Tests = []
        };
}