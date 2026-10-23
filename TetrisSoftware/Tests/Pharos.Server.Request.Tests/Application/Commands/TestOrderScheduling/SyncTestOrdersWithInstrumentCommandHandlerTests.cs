
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using EntityState = Pharos.Server.Application.Request.EntityState;

namespace Pharos.Server.Request.Application.Commands.TestOrderScheduling;

[Collection(nameof(RequestTestsCollection))]
public class SyncTestOrdersWithInstrumentCommandHandlerTests(RequestTestsWaf factory) : DatabaseTestBase<RequestTestsWaf>(factory)
{
    private const int _tubeDefinitionIdVolumeCheck = 1;
    private const int _tubeDefinitionIdNoVolumeCheck = 2;
    private const string _method1 = "M1";
    private const string _method2 = "M2";
    private const string _method3 = "M3";
    private const string _test1 = "T1";
    private const string _test2 = "T2";
    private const string _test3 = "T3";

    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<SyncTestOrdersWithInstrumentCommandHandler>> _loggerMock = new();
    private readonly Mock<IOptionsSnapshot<TubeTypesSettings>> _tubeSettingsMock = new();
    private SyncTestOrdersWithInstrumentCommandHandler _sut = null!;

    #region CancelTestOrders Tests

    [Theory]
    [InlineData(TestOrderState.Scheduled, false, true)]
    [InlineData(TestOrderState.UnableToStart, false, true)]
    [InlineData(TestOrderState.Started, false, false)]
    [InlineData(TestOrderState.Scheduled, true, false)]
    [InlineData(TestOrderState.Scheduled, false, false, EntityState.Unchanged)]
    public async Task Handle_Should_CancelDeactivatedTestOrders(TestOrderState state, bool isActive, bool cancelled, EntityState entityState = EntityState.Modified)
    {
        // Arrange
        var testOrderId = Guid.NewGuid();
        var request = new Domain.Request();

        request.TestOrders.Add(new TestOrder
        {
            Uid = testOrderId,
            InstrumentNumber = "INST1",
            State = state,
            IsActive = isActive,
            EntityState = entityState,
        });

        var command = new SyncTestOrdersWithInstrumentCommand(request);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetProcessableSampleTubesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SampleTubeDto>());

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        if (cancelled)
        {
            _mediatorMock.Verify(
                m => m.Send(
                    It.Is<DeleteTestOrderCommand>(c =>
                        c.InstrumentNumber == "INST1"
                        && c.TestOrderRequest.TestOrderIds.Contains(testOrderId.ToString())),
                    It.IsAny<CancellationToken>()),
                Times.Once());
        }
        else
        {
            _mediatorMock.Verify(m => m.Send(It.IsAny<DeleteTestOrderCommand>(), It.IsAny<CancellationToken>()), Times.Never());
        }
    }

    [Fact]
    public async Task Handle_Should_CancelDeletedTestOrders_When_DeletedTestOrdersProvided()
    {
        // Arrange
        var testOrderId1 = Guid.NewGuid();
        var testOrderId2 = Guid.NewGuid();
        var request = new Domain.Request();

        var deletedTestOrders = new List<TestOrder> {
            new()
            {
                Uid = testOrderId1,
                InstrumentNumber = "INST1",
                State = TestOrderState.Scheduled,
            },
            new()
            {
                Uid = testOrderId2,
                InstrumentNumber = null,
                State = TestOrderState.Scheduled,
            }
        };
        var command = new SyncTestOrdersWithInstrumentCommand(request, deletedTestOrders);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetProcessableSampleTubesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SampleTubeDto>());

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<DeleteTestOrderCommand>(c =>
                    c.InstrumentNumber == "INST1"
                    && c.TestOrderRequest.TestOrderIds.Contains(testOrderId1.ToString())
                    && !c.TestOrderRequest.TestOrderIds.Contains(testOrderId2.ToString())),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ContinueProcessing_When_CancelTestOrdersFails()
    {
        // Arrange
        var testOrderId = Guid.NewGuid();
        var request = new Domain.Request();

        request.TestOrders.Add(new TestOrder
        {
            Uid = testOrderId,
            InstrumentNumber = "INST1",
            State = TestOrderState.Scheduled,
            IsActive = false,
            EntityState = EntityState.Modified
        });

        var command = new SyncTestOrdersWithInstrumentCommand(request);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteTestOrderCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetProcessableSampleTubesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SampleTubeDto>());

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Should log error but continue
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Failed to cancel test orders")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mediatorMock.Verify(
            m => m.Send(It.IsAny<GetProcessableSampleTubesQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_CancelBothDeactivatedAndDeletedTestOrders_When_BothAreProvided()
    {
        // Arrange
        var testOrderId1 = Guid.NewGuid();
        var testOrderId2 = Guid.NewGuid();
        var testOrderId3 = Guid.NewGuid();
        var request = new Domain.Request();

        request.TestOrders.Add(new TestOrder
        {
            Uid = testOrderId1,
            InstrumentNumber = "INST1",
            State = TestOrderState.Scheduled,
            IsActive = false,
            EntityState = EntityState.Modified
        });

        var deletedTestOrders = new List<TestOrder> {
            new()
            {
                Uid = testOrderId2,
                InstrumentNumber = "INST1",
                State = TestOrderState.Scheduled,
            },
            new()
            {
                Uid = testOrderId3,
                InstrumentNumber = "INST2",
                State = TestOrderState.Scheduled,
            }
        };
        var command = new SyncTestOrdersWithInstrumentCommand(request, deletedTestOrders);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetProcessableSampleTubesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SampleTubeDto>());

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<DeleteTestOrderCommand>(c =>
                    c.InstrumentNumber == "INST1"
                    && c.TestOrderRequest.TestOrderIds.Contains(testOrderId1.ToString())
                    && c.TestOrderRequest.TestOrderIds.Contains(testOrderId2.ToString())),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            m => m.Send(
                It.Is<DeleteTestOrderCommand>(c =>
                    c.InstrumentNumber == "INST2"
                    && c.TestOrderRequest.TestOrderIds.Contains(testOrderId3.ToString())),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region CheckSampleVolume Tests


    [Theory]
    [InlineData(_tubeDefinitionIdVolumeCheck, SampleVolumeCheck.Off)]
    [InlineData(_tubeDefinitionIdNoVolumeCheck, SampleVolumeCheck.OnRunAccordingToTestOrder)]
    [InlineData(_tubeDefinitionIdNoVolumeCheck, SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow)]
    public async Task Handle_Should_NotCheckVolume_When_VolumeCheckIsNotNeeded(int sampleTubeType, SampleVolumeCheck sampleVolumeCheck)
    {
        // Arrange
        var testOrder = CreateTestOrder(_method1, _test1, 1);

        var request = new Domain.Request
        {
            SampleId = Guid.NewGuid().ToString().Substring(0, Constants.SampleIdMaxLength),
            SampleVolumeCheck = sampleVolumeCheck,
            SampleTubeType = sampleTubeType,
            TestOrders = [testOrder]
        };

        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        dbContext.Requests.Add(request);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([_method1]);

        var tube = new SampleTubeDto("TUBE1", "INST1", 10, SampleTubeIdentitySource.BarcodeScanning); // insufficient volume
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetNeededSampleVolumeQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateTestOrderCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(SampleVolumeCheck.OnRunAccordingToTestOrder)]
    [InlineData(SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow)]
    public async Task Handle_Should_NotUpdateTestOrder_When_VolumeIsNotMeasured(SampleVolumeCheck sampleVolumeCheck)
    {
        // Arrange
        var testOrder = CreateTestOrder(_method1, _test1, 1);

        var request = new Domain.Request
        {
            SampleId = Guid.NewGuid().ToString().Substring(0, Constants.SampleIdMaxLength),
            SampleVolumeCheck = sampleVolumeCheck,
            SampleTubeType = _tubeDefinitionIdVolumeCheck,
            TestOrders = [testOrder]
        };
        var tube = new SampleTubeDto("TUBE1", "INST1", 0, SampleTubeIdentitySource.BarcodeScanning); // not measured or empty
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetNeededSampleVolumeQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateTestOrderCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(m => m.Publish(It.IsAny<NotEnoughSampleVolumeEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(SampleVolumeCheck.OnRunAccordingToTestOrder)]
    [InlineData(SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow)]
    public async Task Handle_Should_SendAllTestOrders_When_SampleVolumeIsSufficient(SampleVolumeCheck sampleVolumeCheck)
    {
        // Arrange
        var testOrder1 = CreateTestOrder(_method1, _test1, 1);
        var testOrder2 = CreateTestOrder(_method2, _test2, 2);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleVolumeCheck = sampleVolumeCheck,
            SampleTubeType = _tubeDefinitionIdVolumeCheck,
            TestOrders = [testOrder1, testOrder2]
        };

        // all methods active
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([_method1, _method2]);

        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        dbContext.Requests.Add(request);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        MockNeededSampleVolumes([100, 200]);

        var instrumentNumber = "INST1";
        var tube = new SampleTubeDto("TUBE1", instrumentNumber, 1000, SampleTubeIdentitySource.BarcodeScanning); // Sufficient volume
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetNeededSampleVolumeQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<UpdateTestOrderCommand>(c =>
                    c.InstrumentNumber == instrumentNumber &&
                    c.TestOrderRequest.Samples[0].TestOrders.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(SampleVolumeCheck.OnRunAccordingToTestOrder, 2)]
    [InlineData(SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow, 0)]
    public async Task Handle_Should_NotSendTests_Which_DoNotHaveEnoughSampleVolume(SampleVolumeCheck sampleVolumeCheck, int numberOfSentTests)
    {
        // Arrange
        var testOrder1 = CreateTestOrder(_method1, _test1, 1);
        var testOrder2 = CreateTestOrder(_method2, _test2, 2);
        var testOrder3 = CreateTestOrder(_method3, _test3, 3);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleVolumeCheck = sampleVolumeCheck,
            SampleTubeType = _tubeDefinitionIdVolumeCheck,
            TestOrders = [testOrder1, testOrder2, testOrder3]
        };

        // all methods active
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([_method1, _method2, _method3]);

        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        dbContext.Requests.Add(request);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        MockNeededSampleVolumes([100, 200, 300]);

        var instrumentNumber = "INST1";
        var tube = new SampleTubeDto("TUBE1", instrumentNumber, 250, SampleTubeIdentitySource.BarcodeScanning); // Only enough for 2 tests
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetNeededSampleVolumeQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        if (numberOfSentTests == 0)
        {
            _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateTestOrderCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        else
        {

            _mediatorMock.Verify(
                m => m.Send(
                    It.Is<UpdateTestOrderCommand>(c =>
                        c.InstrumentNumber == instrumentNumber &&
                        c.TestOrderRequest.Samples[0].TestOrders.Count == numberOfSentTests),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        request.TestOrders.Count(x => x.State == TestOrderState.Scheduled).Should().Be(numberOfSentTests);
    }

    [Theory]
    [InlineData(SampleVolumeCheck.OnRunAccordingToTestOrder)]
    [InlineData(SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow)]
    public async Task Handle_Should_PublishNotEnoughVolumeEvent_When_NoTestsCanRun(SampleVolumeCheck sampleVolumeCheck)
    {
        // Arrange
        var testOrder1 = CreateTestOrder(_method1, _test1, 1);
        var testOrder2 = CreateTestOrder(_method2, _test2, 2);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleVolumeCheck = sampleVolumeCheck,
            SampleTubeType = _tubeDefinitionIdVolumeCheck,
            TestOrders = [testOrder1, testOrder2]
        };

        // all methods active
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([_method1, _method2]);

        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        dbContext.Requests.Add(request);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        MockNeededSampleVolumes([100, 200]);

        var instrumentNumber = "INST1";
        var tubeId = "TUBE1";
        var tube = new SampleTubeDto(tubeId, instrumentNumber, 50, SampleTubeIdentitySource.BarcodeScanning); // insufficient volume
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetNeededSampleVolumeQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateTestOrderCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<NotEnoughSampleVolumeEvent>(e =>
                    e.InstrumentNumber == instrumentNumber && e.TubeId == tubeId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(SampleVolumeCheck.OnRunAccordingToTestOrder)]
    [InlineData(SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow)]
    public async Task Handle_Should_IncludeStartedTestsInVolumeCalculation(SampleVolumeCheck sampleVolumeCheck)
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleVolumeCheck = sampleVolumeCheck,
            SampleTubeType = _tubeDefinitionIdVolumeCheck,
            TestOrders = [
                CreateTestOrder(_method1, _test1, 1, TestOrderState.Started, "TUBE1", "INST1"),
                CreateTestOrder(_method1, _test1, 3, TestOrderState.Scheduled, "TUBE1", "INST1"),
                CreateTestOrder(_method1, _test1, 2, TestOrderState.UnableToStart, "TUBE1", "INST1"),
                CreateTestOrder(_method1, _test1, 4)
            ]
        };

        var testOrderIds = request.TestOrders.Select(t => t.Uid.ToString()).ToList();

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([_method1]);

        var instrumentNumber = "INST1";
        var tube = new SampleTubeDto("TUBE1", instrumentNumber, 10, SampleTubeIdentitySource.BarcodeScanning); // volume does not matter
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Should query volume with all tests
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<GetNeededSampleVolumeQuery>(q => q.InstrumentNumber == instrumentNumber
                                                       && q.TestOrders.All(t => testOrderIds.Any(id => id == t.TestOrderId))
                                                       && q.TestOrders[1].TestOrderId == testOrderIds[1]), // Should sort by test order when sending to volume query
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(SampleVolumeCheck.OnRunAccordingToTestOrder, 1)]
    [InlineData(SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow, 2)]
    public async Task Handle_Should_CancelScheduledTest_When_VolumeIsInsufficient(SampleVolumeCheck sampleVolumeCheck, int numberOfCancelledTests)
    {
        // Arrange
        var scheduledTest1 = CreateTestOrder(_method1, _test1, 1, TestOrderState.Scheduled, "TUBE1", "INST1");
        var notStartedTest1 = CreateTestOrder(_method1, _test1, 2);
        var scheduledTest2 = CreateTestOrder(_method1, _test1, 3, TestOrderState.Scheduled, "TUBE1", "INST1");
        var notStartedTest2 = CreateTestOrder(_method1, _test1, 4);

        List<string> scheduledTestIds = [scheduledTest1.Uid.ToString(), scheduledTest2.Uid.ToString()];

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleVolumeCheck = sampleVolumeCheck,
            SampleTubeType = _tubeDefinitionIdVolumeCheck,
            TestOrders = [scheduledTest2, notStartedTest1, scheduledTest1, notStartedTest2] // out of order to test that it cancels the correct one based on sort order, not list order
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([_method1]);

        MockNeededSampleVolumes([100, 200, 300, 400]);

        var instrumentNumber = "INST1";
        var tube = new SampleTubeDto("TUBE1", instrumentNumber, 200, SampleTubeIdentitySource.BarcodeScanning); // only enough for two tests
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Should cancel the new test (last one) since it doesn't fit
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetNeededSampleVolumeQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<DeleteTestOrderCommand>(c =>
                    c.InstrumentNumber == instrumentNumber
                    && c.TestOrderRequest.TestOrderIds.Count == numberOfCancelledTests
                    && c.TestOrderRequest.TestOrderIds.All(x => scheduledTestIds.Contains(x))),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(SampleVolumeCheck.OnRunAccordingToTestOrder, false)]
    [InlineData(SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow, true)]
    public async Task Handle_Should_SetDontStartFlagCorrectly_Based_OnSampleVolumeCheckMode(SampleVolumeCheck sampleVolumeCheck, bool expectedFlag)
    {
        // Arrange
        var testOrder = CreateTestOrder(_method1, _test1, 1);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleVolumeCheck = sampleVolumeCheck,
            SampleTubeType = _tubeDefinitionIdVolumeCheck,
            TestOrders = [testOrder]
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([_method1]);

        MockNeededSampleVolumes([100]);

        var instrumentNumber = "INST1";
        var tube = new SampleTubeDto("TUBE1", instrumentNumber, 1000, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _mediatorMock.Verify(
            m => m.Send(
                It.Is<UpdateTestOrderCommand>(c =>
                    c.TestOrderRequest.Samples[0].DoNotStartIfVolumeNotEnough == expectedFlag),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Inactive Methods Tests

    [Fact]
    public async Task Handle_Should_PublishNoActiveMethodsEvent_When_NeededMethodsAreInactive()
    {
        // Arrange
        var testOrder = CreateTestOrder(_method1, _test1, 1);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [testOrder]
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([_method2]);

        var instrumentNumber = "INST1";
        var tubeId = "TUBE1";
        var tube = new SampleTubeDto(tubeId, instrumentNumber, 100, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube, true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<NoActiveMethodsForTubeEvent>(e =>
                    e.InstrumentNumber == instrumentNumber &&
                    e.TubeIdList.Contains(tubeId)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateTestOrderCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_NotPublishNoActiveMethodsEvent_When_AtLeastOneMethodIsActive()
    {
        // Arrange
        var testOrder1 = CreateTestOrder(_method1, _test1, 1);
        var testOrder2 = CreateTestOrder(_method2, _test2, 2);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [testOrder1, testOrder2]
        };

        // Only method1 is active
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string> { _method1 });

        var instrumentNumber = "INST1";
        var tube = new SampleTubeDto("TUBE1", instrumentNumber, 100, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<NoActiveMethodsForTubeEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateTestOrderCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_SendOnlyTestsWithActiveMethods_When_SomeMethodsAreInactive()
    {
        // Arrange
        var activeTest1 = CreateTestOrder(_method1, _test1, 1);
        var inactiveTest = CreateTestOrder(_method2, _test2, 2);
        var activeTest2 = CreateTestOrder(_method3, _test3, 3);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [activeTest1, inactiveTest, activeTest2]
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string> { _method1, _method3 });

        var instrumentNumber = "INST1";
        var tube = new SampleTubeDto("TUBE1", instrumentNumber, 100, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<UpdateTestOrderCommand>(c =>
                    c.InstrumentNumber == instrumentNumber
                    && c.TestOrderRequest.Samples[0].TestOrders.Count == 2
                    && c.TestOrderRequest.Samples[0].TestOrders.All(t => t.MethodName == _method1 || t.MethodName == _method3)
                    && c.TestOrderRequest.Samples[0].TestOrders.All(t => t.MethodName != _method2)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(TestOrderState.Scheduled)]
    [InlineData(TestOrderState.Started)]
    [InlineData(TestOrderState.UnableToStart)]
    [InlineData(TestOrderState.Approved)]
    public async Task Handle_Should_NotPublishNoActiveMethodsEvent_When_TubeHasStartedTests(TestOrderState existingTestState)
    {
        // Arrange
        var existingTest = CreateTestOrder(_method1, _test1, 1, existingTestState, "TUBE1", "INST1");
        var notStartedTest = CreateTestOrder(_method2, _test2, 2);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [existingTest, notStartedTest]
        };

        // No active methods
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());

        var tube = new SampleTubeDto("TUBE1", "INST1", 100, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Should not publish event because tube has non-NotStarted tests
        _mediatorMock.Verify(m => m.Publish(It.IsAny<NoActiveMethodsForTubeEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_NotSendUpdateAndNotPublishNoActiveMethodsEvent_When_HasScheduledTestAndInactiveNotStartedTest()
    {
        // Arrange
        var scheduledTest = CreateTestOrder(_method1, _test1, 1, TestOrderState.Scheduled, "TUBE1", "INST1");
        scheduledTest.EntityState = EntityState.Unchanged;

        var inactiveNotStartedTest = CreateTestOrder(_method2, _test2, 2);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [scheduledTest, inactiveNotStartedTest]
        };

        // Only method for already scheduled test is active, not-started test method is inactive
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string> { _method1 });

        var tube = new SampleTubeDto("TUBE1", "INST1", 100, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateTestOrderCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(m => m.Publish(It.IsAny<NoActiveMethodsForTubeEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_GroupNoActiveMethodsEventByInstrument_When_MultipleTubesAffected()
    {
        // Arrange
        var testOrder1 = CreateTestOrder(_method1, _test1, 1);
        var testOrder2 = CreateTestOrder(_method2, _test2, 2);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [testOrder1, testOrder2]
        };

        // No active methods on any instrument
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());

        var tube1 = new SampleTubeDto("TUBE1", "INST1", 100, SampleTubeIdentitySource.BarcodeScanning);
        var tube2 = new SampleTubeDto("TUBE2", "INST1", 100, SampleTubeIdentitySource.BarcodeScanning);
        var tube3 = new SampleTubeDto("TUBE3", "INST2", 100, SampleTubeIdentitySource.BarcodeScanning);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProcessableSampleTubesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SampleTubeDto> { tube1, tube2, tube3 });

        var command = new SyncTestOrdersWithInstrumentCommand(request, PublishRequestUpdatedEvent: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Should group by instrument
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<NoActiveMethodsForTubeEvent>(e =>
                    e.InstrumentNumber == "INST1" &&
                    e.TubeIdList.Contains("TUBE1") &&
                    e.TubeIdList.Contains("TUBE2")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<NoActiveMethodsForTubeEvent>(e =>
                    e.InstrumentNumber == "INST2" &&
                    e.TubeIdList.Contains("TUBE3")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateTestOrderCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region FullUpdateNeeded Tests

    [Theory]
    [InlineData(TestOrderState.Scheduled)]
    [InlineData(TestOrderState.UnableToStart)]
    public async Task Handle_Should_SendFullUpdate_When_ScheduledTestIsModified(TestOrderState state)
    {
        // Arrange
        var instrumentNumber = "INST1";
        var tubeId = "TUBE1";
        var scheduledTest1 = CreateTestOrder(_method1, _test1, 1, state, tubeId, instrumentNumber);
        scheduledTest1.EntityState = EntityState.Modified; // Modified scheduled test
        var scheduledTest2 = CreateTestOrder(_method1, _test1, 1, state, tubeId, instrumentNumber);

        var notStartedTest = CreateTestOrder(_method2, _test2, 2);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [scheduledTest1, scheduledTest2, notStartedTest]
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_method1, _method2]);

        var command = new SyncTestOrdersWithInstrumentCommand(request, null, new SampleTubeDto(tubeId, instrumentNumber, 0, SampleTubeIdentitySource.BarcodeScanning));

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Should send both scheduled and not-started tests (full update)
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<UpdateTestOrderCommand>(c =>
                    c.InstrumentNumber == instrumentNumber &&
                    c.TestOrderRequest.Samples[0].TestOrders.Count == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_SendFullUpdate_When_NotStartedTestHasLowerSortOrderThanScheduled()
    {
        // Arrange
        var scheduledTest = CreateTestOrder(_method1, _test1, 2, TestOrderState.Scheduled, "TUBE1", "INST1"); // Higher sort order
        var notStartedTest = CreateTestOrder(_method2, _test2, 1); // Lower sort order - should run first

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [scheduledTest, notStartedTest]
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_method1, _method2]);

        var instrumentNumber = "INST1";
        var tube = new SampleTubeDto("TUBE1", instrumentNumber, 1000, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Should send both tests because new test should run before scheduled test
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<UpdateTestOrderCommand>(c =>
                    c.InstrumentNumber == instrumentNumber &&
                    c.TestOrderRequest.Samples[0].TestOrders.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_SendOnlyNotStartedTests_When_ScheduledTestNotModifiedAndSortOrderCorrect()
    {
        // Arrange
        var scheduledTest = CreateTestOrder(_method1, _test1, 1, TestOrderState.Scheduled, "TUBE1", "INST1");
        scheduledTest.EntityState = EntityState.Unchanged; // Not modified

        var notStartedTest = CreateTestOrder(_method2, _test2, 2); // Higher sort order

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [scheduledTest, notStartedTest]
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_method1, _method2]);

        var instrumentNumber = "INST1";
        var tube = new SampleTubeDto("TUBE1", instrumentNumber, 1000, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Should send only the not-started test (incremental update)
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<UpdateTestOrderCommand>(c =>
                    c.InstrumentNumber == instrumentNumber &&
                    c.TestOrderRequest.Samples[0].TestOrders.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);

        notStartedTest.State.Should().Be(TestOrderState.Scheduled);
    }


    [Fact]
    public async Task Handle_Should_OnlyIncludeScheduledTestsForSameTube_When_FullUpdateNeeded()
    {
        // Arrange
        var scheduledTestSameTube = CreateTestOrder(_method1, _test1, 1, TestOrderState.Scheduled, "TUBE1", "INST1");
        scheduledTestSameTube.EntityState = EntityState.Modified;

        var scheduledTestDifferentTube = CreateTestOrder(_method2, _test2, 2, TestOrderState.Scheduled, "TUBE2", "INST1");

        var notStartedTest = CreateTestOrder(_method3, _test3, 3);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [scheduledTestSameTube, scheduledTestDifferentTube, notStartedTest]
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_method1, _method2, _method3]);

        var instrumentNumber = "INST1";
        var tube = new SampleTubeDto("TUBE1", instrumentNumber, 1000, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Should send only tests for TUBE1 (scheduledTestSameTube + notStartedTest)
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<UpdateTestOrderCommand>(c =>
                    c.InstrumentNumber == instrumentNumber &&
                    c.TestOrderRequest.Samples[0].TestOrders.Count == 2 &&
                    c.TestOrderRequest.Samples[0].Sample.TubeId == "TUBE1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Request update after UpdateTestOrder

    [Fact]
    public async Task Handle_Should_SaveChangesAndPublishEvent_When_NotStartedTestsAreScheduled()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [
                CreateTestOrder(_method1, _test1, 1),
                CreateTestOrder(_method2, _test2, 2)]
        };

        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        dbContext.Requests.Add(request);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        request.RequestState.Should().Be(RequestState.NotStarted);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_method1, _method2]);

        var instrumentNumber = "INST1";
        var tubeId = "TUBE1";
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, new SampleTubeDto(tubeId, instrumentNumber, 0, SampleTubeIdentitySource.BarcodeScanning), true);
        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Should publish RequestUpdatedEvent
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<RequestUpdatedEvent>(e =>
                    e.RequestId == request.Id &&
                    e.TestOrders.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);

        await using var scope = ServiceProvider.CreateAsyncScope();
        dbContext = scope.ServiceProvider.GetRequiredService<IRequestDbContext>();

        var updatedRequest = await dbContext.Requests
            .Where(r => r.Id == request.Id)
            .Include(r => r.TestOrders)
            .FirstAsync(TestContext.Current.CancellationToken);

        updatedRequest.RequestState.Should().Be(RequestState.Started);
        updatedRequest.TestOrders.Count.Should().Be(2);

        var test = updatedRequest.TestOrders[0];
        test.State.Should().Be(TestOrderState.Scheduled);
        test.InstrumentNumber.Should().Be(instrumentNumber);
        test.TubeId.Should().Be(tubeId);

        test = updatedRequest.TestOrders[1];
        test.State.Should().Be(TestOrderState.Scheduled);
        test.InstrumentNumber.Should().Be(instrumentNumber);
        test.TubeId.Should().Be(tubeId);
    }

    [Fact]
    public async Task Handle_Should_NotPublishEvent_When_OnlyScheduledTestsAreResent()
    {
        // Arrange
        var scheduledTest = CreateTestOrder(_method1, _test1, 1, TestOrderState.Scheduled, "TUBE1", "INST1");
        scheduledTest.EntityState = EntityState.Modified; // Trigger full update

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [scheduledTest]
        };

        var tube = new SampleTubeDto("TUBE1", "INST1", 0, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Should not publish RequestUpdatedEvent because no NotStarted tests were updated
        _mediatorMock.Verify(m => m.Publish(It.IsAny<RequestUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);

        scheduledTest.State.Should().Be(TestOrderState.Scheduled); // Remains scheduled
    }

    [Fact]
    public async Task Handle_Should_NotPublishEvent_When_UpdateTestOrderCommandReturnsFalse()
    {
        // Arrange
        var testOrder = CreateTestOrder(_method1, _test1, 1);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [testOrder]
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_method1]);

        // UpdateTestOrderCommand returns false (not sent)
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateTestOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var tube = new SampleTubeDto("TUBE1", "INST1", 1000, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Should not publish RequestUpdatedEvent because sent == false
        _mediatorMock.Verify(m => m.Publish(It.IsAny<RequestUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);

        testOrder.State.Should().Be(TestOrderState.NotStarted); // State not updated
    }

    [Fact]
    public async Task Handle_Should_PublishEventOnlyOnce_When_MultipleTestsScheduledInMultipleTubes()
    {
        // Arrange
        var testOrder1 = CreateTestOrder(_method1, _test1, 1);
        var testOrder2 = CreateTestOrder(_method2, _test2, 2);

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [testOrder1, testOrder2]
        };

        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        dbContext.Requests.Add(request);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_method1, _method2]);

        var tube1 = new SampleTubeDto("TUBE1", "INST1", 1000, SampleTubeIdentitySource.BarcodeScanning);
        var tube2 = new SampleTubeDto("TUBE2", "INST2", 1000, SampleTubeIdentitySource.BarcodeScanning);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProcessableSampleTubesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SampleTubeDto> { tube1, tube2 });

        var command = new SyncTestOrdersWithInstrumentCommand(request, PublishRequestUpdatedEvent: true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Should publish RequestUpdatedEvent only once, even though multiple tubes processed
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<RequestUpdatedEvent>(e => e.RequestId == request.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_PublishEventWithAllTestOrders_When_RequestUpdated()
    {
        // Arrange
        var notStartedTest = CreateTestOrder(_method1, _test1, 1);
        var scheduledTest = CreateTestOrder(_method2, _test2, 2, TestOrderState.Scheduled, "TUBE2", "INST2");

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [notStartedTest, scheduledTest]
        };

        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        dbContext.Requests.Add(request);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_method1]);

        var tube = new SampleTubeDto("TUBE1", "INST1", 1000, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube, true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert - Event should include all test orders from request
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<RequestUpdatedEvent>(e =>
                    e.RequestId == request.Id &&
                    e.TestOrders.Count == 2 && // Both NotStarted (now Scheduled) and already Scheduled
                    e.TestOrders.Any(t => t.Uid == notStartedTest.Uid) &&
                    e.TestOrders.Any(t => t.Uid == scheduledTest.Uid)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region TestOrderRequest mapping tests

    [Fact]
    public async Task Handle_Should_UseFirstCustomRunId_When_TestsHaveCustomParameters()
    {
        // Arrange
        var testOrder1 = CreateTestOrder(_method1, _test1, 1);
        testOrder1.TestOrderCustomParameters = new TestOrderCustomParameter { CustomRunId = "FIRST_CUSTOM" };

        var testOrder2 = CreateTestOrder(_method2, _test2, 2);
        testOrder2.TestOrderCustomParameters = new TestOrderCustomParameter { CustomRunId = "SECOND_CUSTOM" };

        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [testOrder1, testOrder2]
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_method1, _method2]);

        var tube = new SampleTubeDto("TUBE1", "INST1", 1000, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<UpdateTestOrderCommand>(c =>
                    c.TestOrderRequest.CustomRunId == "FIRST_CUSTOM"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_SetSampleInformationCorrectly_From_Request()
    {
        // Arrange
        var sampleId = "SAMPLE123";
        var preDilutionFactor = 2;
        var createdDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        var testOrder = CreateTestOrder(_method1, _test1, 1);

        var request = new Domain.Request
        {
            SampleId = sampleId,
            Created = createdDate,
            PreDilutionFactor = preDilutionFactor,
            HighPriority = false,
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [testOrder]
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_method1]);

        var instrumentNumber = "INST1";
        var tubeId = "TUBE1";
        var tube = new SampleTubeDto(tubeId, instrumentNumber, 1000, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<UpdateTestOrderCommand>(c =>
                    c.InstrumentNumber == instrumentNumber &&
                    c.TestOrderRequest.Samples[0].Sample.SampleId == sampleId &&
                    c.TestOrderRequest.Samples[0].Sample.SampleDate == Timestamp.FromDateTime(createdDate) &&
                    c.TestOrderRequest.Samples[0].Sample.HighPriority == false &&
                    c.TestOrderRequest.Samples[0].Sample.PreDilutionFactor == preDilutionFactor &&
                    c.TestOrderRequest.Samples[0].Sample.TubeId == tubeId &&
                    c.TestOrderRequest.Samples[0].Sample.TubeTypeIndex == _tubeDefinitionIdNoVolumeCheck),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_SetHighPriorityToTrue_When_RequestHighPriorityIsTrue()
    {
        // Arrange
        var testOrder = CreateTestOrder(_method1, _test1, 1);
        testOrder.IsReflexTest = false;

        var request = new Domain.Request
        {
            SampleId = "S1",
            HighPriority = true,
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [testOrder]
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_method1]);

        var tube = new SampleTubeDto("TUBE1", "INST1", 1000, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<UpdateTestOrderCommand>(c =>
                    c.TestOrderRequest.Samples[0].Sample.HighPriority == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_SetHighPriorityToTrue_When_AnyTestIsReflexTest()
    {
        // Arrange
        var testOrder1 = CreateTestOrder(_method1, _test1, 1);
        testOrder1.IsReflexTest = false;

        var testOrder2 = CreateTestOrder(_method2, _test2, 2);
        testOrder2.IsReflexTest = true;

        var request = new Domain.Request
        {
            SampleId = "S1",
            HighPriority = false,
            SampleTubeType = _tubeDefinitionIdNoVolumeCheck,
            TestOrders = [testOrder1, testOrder2]
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveInstrumentMethodsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_method1, _method2]);

        var tube = new SampleTubeDto("TUBE1", "INST1", 1000, SampleTubeIdentitySource.BarcodeScanning);
        var command = new SyncTestOrdersWithInstrumentCommand(request, null, tube);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<UpdateTestOrderCommand>(c =>
                    c.TestOrderRequest.Samples[0].Sample.HighPriority == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateTestOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var requestSettings = new TubeTypesSettings
        {
            TubeTypeList =
            [
                new()
                {
                    TubeDefinitionId = _tubeDefinitionIdVolumeCheck,
                    Name = "Standard",
                    VolumeCalculation = true
                },
                new()
                {
                    TubeDefinitionId = _tubeDefinitionIdNoVolumeCheck,
                    Name = "Non-Standard",
                    VolumeCalculation = false
                }
            ],
            NormalTubeDefinitionId = _tubeDefinitionIdVolumeCheck.ToString()
        };

        _tubeSettingsMock.Setup(x => x.Value).Returns(requestSettings);

        // Create test methods and tests in the database
        try
        {
            await using var scope = ServiceProvider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IParametersDbContext>();
            await db.CreateMethod(methodName: _method1, testNames: [_test1]);
            await db.CreateMethod(methodName: _method2, testNames: [_test2]);
            await db.CreateMethod(methodName: _method3, testNames: [_test3]);
        }
        catch
        {
            // Ignore if already exists from another test run
        }

        var methods = ServiceProvider.GetRequiredService<IMethodsService>();
        var articles = ServiceProvider.GetRequiredService<IArticlesService>();
        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();

        _sut = new SyncTestOrdersWithInstrumentCommandHandler(
            dbContext,
            _tubeSettingsMock.Object,
            methods,
            articles,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    private static TestOrder CreateTestOrder(string methodName, string testName, int sortOrder, TestOrderState state = TestOrderState.NotStarted, string? tubeId = null, string? instrumentNumber = null)
    {
        return new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = methodName,
            TestName = testName,
            TestSortOrder = sortOrder,
            State = state,
            TubeId = tubeId,
            InstrumentNumber = instrumentNumber
        };
    }

    private void MockNeededSampleVolumes(int[] neededVolumes)
    {
        var volumeResponse = new Tetris.Protocol.Instrument.CalculateNeededSampleVolumeResponse();
        foreach (var volume in neededVolumes)
        {
            volumeResponse.NeededSampleVolumes.Add(new Tetris.Protocol.Instrument.NeededSampleVolume
            {
                NeededSampleVolumeAccumulated = volume
            });
        }

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetNeededSampleVolumeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(volumeResponse);
    }

    #endregion
}