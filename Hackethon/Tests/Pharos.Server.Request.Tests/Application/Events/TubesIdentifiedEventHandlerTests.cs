using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Infrastructure;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;

namespace Pharos.Server.Request.Application.Events;

public class TubesIdentifiedEventHandlerTests
{
    private static TubesIdentifiedEventHandler CreateHandler(
        Mock<IRequestDbContext> dbContextMock,
        out Mock<IMediator> mediatorMock,
        RequestSettings? requestSettings = null,
        TubeTypesSettings? tubeSettings = null,
        Mock<IDateTime>? dateTimeMock = null,
        Mock<ILogger<TubesIdentifiedEventHandler>>? loggerMock = null)
    {
        requestSettings ??= new RequestSettings();
        tubeSettings ??= new TubeTypesSettings
        {
            TubeTypeList =
            [
                new TubeType
                {
                    TubeDefinitionId = 1,
                    Name = "DefaultTube",
                    VolumeCalculation = false
                }
            ],
            NormalTubeDefinitionId = "1"
        };

        var requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        requestSettingsMock.Setup(x => x.Value).Returns(requestSettings);

        var tubeSettingsMock = new Mock<IOptionsSnapshot<TubeTypesSettings>>();
        tubeSettingsMock.Setup(x => x.Value).Returns(tubeSettings);

        mediatorMock = new Mock<IMediator>();

        dateTimeMock ??= new Mock<IDateTime>();
        dateTimeMock.Setup(x => x.Now).Returns(DateTimeOffset.UtcNow);

        loggerMock ??= new Mock<ILogger<TubesIdentifiedEventHandler>>();

        return new TubesIdentifiedEventHandler(
            dbContextMock.Object,
            requestSettingsMock.Object,
            tubeSettingsMock.Object,
            dateTimeMock.Object,
            mediatorMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NoExistingRequests_ShouldAskForMissingRequestsAndReturn()
    {
        // Arrange
        var dbContextMock = new Mock<IRequestDbContext>();
        var handler = CreateHandler(dbContextMock, out var mediatorMock);
        var tubeId1 = new SampleTubeId("T1", "S1");
        var tubeId2 = new SampleTubeId("T2", "S2");

        var request = new Domain.Request
        {
            SampleId = tubeId2.SampleId,
        };

        dbContextMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request> { request });
        var notification = new TubesIdentifiedEvent("INST1", [new SampleTubeInformation(tubeId1, SampleTubeIdentitySource.BarcodeScanning), new SampleTubeInformation(tubeId2, SampleTubeIdentitySource.BarcodeScanning)]);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        mediatorMock.Verify(
            m => m.Send(It.Is<AskForMissingTestOrdersCommand>(x => x.SampleIDs.Contains(tubeId1.SampleId) && x.SampleIDs.Contains(tubeId2.SampleId) && !x.ManualScan),
            It.IsAny<CancellationToken>()),
            Times.Once);

        mediatorMock.Verify(
            m => m.Send(It.IsAny<RefreshExistingRequestsCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);

        mediatorMock.Verify(
            m => m.Send(It.IsAny<SyncTestOrdersWithInstrumentCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ReuseSampleId_OldRequest_ShouldAskForMissingRequests()
    {
        // Arrange
        var dbContextMock = new Mock<IRequestDbContext>();
        var tubeId = new SampleTubeId("Tube1", "Sample1");

        var now = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);

        var oldRequest = new Domain.Request
        {
            SampleId = tubeId.SampleId,
            Created = now.AddDays(-11)
        };

        dbContextMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request> { oldRequest });

        var settings = new RequestSettings
        {
            ReuseSampleId = true,
            DaysBeforeReuseOfSampleId = 10
        };

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(x => x.Now).Returns(now);

        var handler = CreateHandler(dbContextMock, out var mediatorMock, requestSettings: settings, dateTimeMock: dateTimeMock);

        var notification = new TubesIdentifiedEvent("INST1", [new SampleTubeInformation(tubeId, SampleTubeIdentitySource.BarcodeScanning)]);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        mediatorMock.Verify(
            m => m.Send(
                It.Is<AskForMissingTestOrdersCommand>(x =>
                    x.SampleIDs.Single() == tubeId.SampleId &&
                    !x.ManualScan),
                It.IsAny<CancellationToken>()),
            Times.Once);

        mediatorMock.Verify(
            m => m.Send(It.IsAny<RefreshExistingRequestsCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);

        mediatorMock.Verify(
            m => m.Send(It.IsAny<SyncTestOrdersWithInstrumentCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_VolumeCalculationEnabled_ShouldMeasureSampleVolumeInsteadOfSendingTests()
    {
        // Arrange
        var tubeTypeIndex = 2;
        var tubeId = new SampleTubeId("Tube1", "Sample1");

        var request = new Domain.Request
        {
            SampleId = tubeId.SampleId,
            SampleTubeType = tubeTypeIndex,
            SampleVolumeCheck = SampleVolumeCheck.OnRunAccordingToTestOrder,
            TestOrders =
            [
                new TestOrder
                {
                    Uid = Guid.NewGuid(),
                    MethodName = "M1",
                    TestName = "T1",
                    State = TestOrderState.NotStarted
                }
            ]
        };

        var dbContextMock = new Mock<IRequestDbContext>();
        dbContextMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request> { request });

        var settings = new TubeTypesSettings
        {
            TubeTypeList =
            [
                new TubeType
                {
                    TubeDefinitionId = tubeTypeIndex,
                    VolumeCalculation = true
                }
            ]
        };

        var handler = CreateHandler(dbContextMock, out var mediatorMock, tubeSettings: settings);


        var notification = new TubesIdentifiedEvent("INST1", [new SampleTubeInformation(tubeId, SampleTubeIdentitySource.BarcodeScanning)]);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        mediatorMock.Verify(
            m => m.Send(
                It.Is<MeasureSampleVolumeCommand>(c =>
                    c.InstrumentNumber == "INST1" &&
                    c.TubeId == tubeId.TubeId &&
                    c.TubeTypeIndex == tubeTypeIndex),
                It.IsAny<CancellationToken>()),
            Times.Once);

        mediatorMock.Verify(
            m => m.Send(It.IsAny<AskForMissingTestOrdersCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);

        mediatorMock.Verify(
            m => m.Send(It.IsAny<RefreshExistingRequestsCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);

        mediatorMock.Verify(
            m => m.Send(It.IsAny<SyncTestOrdersWithInstrumentCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ActiveInstrumentMethods_ShouldSendTestOrdersToInstrument()
    {
        // Arrange
        var tubeId = new SampleTubeId("Tube1", "Sample1");

        var request = new Domain.Request
        {
            SampleId = tubeId.SampleId,
            SampleVolumeCheck = SampleVolumeCheck.Off,
            TestOrders =
            [
                new TestOrder
                {
                    Uid = Guid.NewGuid(),
                    MethodName = "Method1",
                    TestName = "Test1",
                    State = TestOrderState.NotStarted
                }
            ]
        };

        var dbContextMock = new Mock<IRequestDbContext>();
        dbContextMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request> { request });

        var handler = CreateHandler(dbContextMock, out var mediatorMock);

        var notification = new TubesIdentifiedEvent("INST1", [new SampleTubeInformation(tubeId, SampleTubeIdentitySource.BarcodeScanning)]);

        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string> { "Method1" });

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        mediatorMock.Verify(
            m => m.Send(It.Is<SyncTestOrdersWithInstrumentCommand>(x => x.SampleTube!.InstrumentNumber == "INST1"
            && x.SampleTube!.TubeId == tubeId.TubeId && x.Request == request),
            It.IsAny<CancellationToken>()),
            Times.Once);

        mediatorMock.Verify(
            m => m.Send(It.IsAny<AskForMissingTestOrdersCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);

        mediatorMock.Verify(
            m => m.Send(It.Is<RefreshExistingRequestsCommand>(x => x.SampleIDs.First() == tubeId.SampleId && !x.ManualScan),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
