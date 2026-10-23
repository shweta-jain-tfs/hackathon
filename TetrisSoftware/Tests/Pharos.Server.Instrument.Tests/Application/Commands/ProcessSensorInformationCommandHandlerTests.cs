using FluentValidation;
using MediatR;
using Moq;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Instrument.Application.Commands.ProcessSensorInformation;
using Pharos.Server.Instrument.Application.Events;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Commands;

public class ProcessSensorInformationCommandHandlerTests
{
    private const string _instrumentNumber = "instrument-1";

    [Theory]
    [InlineData(DrawerType.Sample1, null, null, null, null, null, LockState.ClosedLocked)]
    [InlineData(DrawerType.Sample1, null, EnumDiscreteSensorType.SampleDrawer1Closed, true, EnumDiscreteSensorType.SampleDrawer1Locked, true, LockState.ClosedLocked)]
    [InlineData(DrawerType.Sample1, null, EnumDiscreteSensorType.SampleDrawer1Closed, true, EnumDiscreteSensorType.SampleDrawer1Locked, false, LockState.ClosedUnlocked)]
    [InlineData(DrawerType.Sample1, null, EnumDiscreteSensorType.SampleDrawer1Closed, false, EnumDiscreteSensorType.SampleDrawer1Locked, false, LockState.Open)]
    [InlineData(DrawerType.Sample1, null, EnumDiscreteSensorType.SampleDrawer1Closed, false, EnumDiscreteSensorType.SampleDrawer1Locked, true, null, true)]
    [InlineData(DrawerType.Sample1, LockState.ClosedLocked, EnumDiscreteSensorType.SampleDrawer1Closed, true, EnumDiscreteSensorType.SampleDrawer1Locked, true, LockState.ClosedLocked)]
    [InlineData(DrawerType.Sample1, LockState.ClosedLocked, EnumDiscreteSensorType.SampleDrawer1Closed, true, EnumDiscreteSensorType.SampleDrawer1Locked, false, LockState.ClosedUnlocked)]
    [InlineData(DrawerType.Sample1, LockState.ClosedLocked, EnumDiscreteSensorType.SampleDrawer1Closed, false, EnumDiscreteSensorType.SampleDrawer1Locked, false, LockState.Open)]
    [InlineData(DrawerType.Sample1, LockState.ClosedLocked, EnumDiscreteSensorType.SampleDrawer1Closed, false, EnumDiscreteSensorType.SampleDrawer1Locked, true, null, true)]
    [InlineData(DrawerType.Sample1, LockState.ClosedUnlocked, EnumDiscreteSensorType.SampleDrawer1Closed, true, EnumDiscreteSensorType.SampleDrawer1Locked, true, LockState.ClosedLocked)]
    [InlineData(DrawerType.Sample1, LockState.ClosedUnlocked, EnumDiscreteSensorType.SampleDrawer1Closed, true, EnumDiscreteSensorType.SampleDrawer1Locked, false, LockState.ClosedUnlocked)]
    [InlineData(DrawerType.Sample1, LockState.ClosedUnlocked, EnumDiscreteSensorType.SampleDrawer1Closed, false, EnumDiscreteSensorType.SampleDrawer1Locked, false, LockState.Open)]
    [InlineData(DrawerType.Sample1, LockState.ClosedUnlocked, EnumDiscreteSensorType.SampleDrawer1Closed, false, EnumDiscreteSensorType.SampleDrawer1Locked, true, null, true)]
    [InlineData(DrawerType.Sample1, LockState.Open, EnumDiscreteSensorType.SampleDrawer1Closed, true, EnumDiscreteSensorType.SampleDrawer1Locked, true, LockState.ClosedLocked)]
    [InlineData(DrawerType.Sample1, LockState.Open, EnumDiscreteSensorType.SampleDrawer1Closed, true, EnumDiscreteSensorType.SampleDrawer1Locked, false, LockState.ClosedUnlocked)]
    [InlineData(DrawerType.Sample1, LockState.Open, EnumDiscreteSensorType.SampleDrawer1Closed, false, EnumDiscreteSensorType.SampleDrawer1Locked, false, LockState.Open)]
    [InlineData(DrawerType.Sample1, LockState.Open, EnumDiscreteSensorType.SampleDrawer1Closed, false, EnumDiscreteSensorType.SampleDrawer1Locked, true, null, true)]
    [InlineData(DrawerType.Sample1, null, EnumDiscreteSensorType.SampleDrawer1Closed, true, null, null, LockState.ClosedLocked)]
    [InlineData(DrawerType.Sample1, null, EnumDiscreteSensorType.SampleDrawer1Closed, false, null, null, null, true)]
    [InlineData(DrawerType.Sample1, LockState.ClosedLocked, EnumDiscreteSensorType.SampleDrawer1Closed, true, null, null, LockState.ClosedLocked)]
    [InlineData(DrawerType.Sample1, LockState.ClosedLocked, EnumDiscreteSensorType.SampleDrawer1Closed, false, null, null, null, true)]
    [InlineData(DrawerType.Sample1, LockState.ClosedUnlocked, EnumDiscreteSensorType.SampleDrawer1Closed, true, null, null, LockState.ClosedUnlocked)]
    [InlineData(DrawerType.Sample1, LockState.ClosedUnlocked, EnumDiscreteSensorType.SampleDrawer1Closed, false, null, null, LockState.Open)]
    [InlineData(DrawerType.Sample1, LockState.Open, EnumDiscreteSensorType.SampleDrawer1Closed, true, null, null, LockState.ClosedUnlocked)]
    [InlineData(DrawerType.Sample1, LockState.Open, EnumDiscreteSensorType.SampleDrawer1Closed, false, null, null, LockState.Open)]
    [InlineData(DrawerType.Sample1, null, null, null, EnumDiscreteSensorType.SampleDrawer1Locked, true, LockState.ClosedLocked)]
    [InlineData(DrawerType.Sample1, null, null, null, EnumDiscreteSensorType.SampleDrawer1Locked, false, LockState.ClosedUnlocked)]
    [InlineData(DrawerType.Sample1, LockState.ClosedLocked, null, null, EnumDiscreteSensorType.SampleDrawer1Locked, true, LockState.ClosedLocked)]
    [InlineData(DrawerType.Sample1, LockState.ClosedLocked, null, null, EnumDiscreteSensorType.SampleDrawer1Locked, false, LockState.ClosedUnlocked)]
    [InlineData(DrawerType.Sample1, LockState.ClosedUnlocked, null, null, EnumDiscreteSensorType.SampleDrawer1Locked, true, LockState.ClosedLocked)]
    [InlineData(DrawerType.Sample1, LockState.ClosedUnlocked, null, null, EnumDiscreteSensorType.SampleDrawer1Locked, false, LockState.ClosedUnlocked)]
    [InlineData(DrawerType.Sample1, LockState.Open, null, null, EnumDiscreteSensorType.SampleDrawer1Locked, true, null, true)]
    [InlineData(DrawerType.Sample1, LockState.Open, null, null, EnumDiscreteSensorType.SampleDrawer1Locked, false, LockState.Open)]
    [InlineData(DrawerType.Sample2, null, null, null, null, null, LockState.ClosedLocked)]
    [InlineData(DrawerType.BottledReagent, null, null, null, null, null, LockState.ClosedUnlocked)]
    [InlineData(DrawerType.Reagent, null, null, null, null, null, LockState.ClosedLocked)]
    [InlineData(DrawerType.RinseWaste, null, null, null, null, null, LockState.ClosedUnlocked)]
    [InlineData(DrawerType.SolidWaste, null, null, null, null, null, LockState.ClosedLocked)]
    [InlineData(DrawerType.Sample2, LockState.Open, EnumDiscreteSensorType.SampleDrawer2Closed, true, EnumDiscreteSensorType.SampleDrawer2Locked, true, LockState.ClosedLocked)]
    [InlineData(DrawerType.BottledReagent, LockState.Open, EnumDiscreteSensorType.BottledReagentDrawerClosed, true, null, null, LockState.ClosedUnlocked)]
    [InlineData(DrawerType.Reagent, LockState.Open, EnumDiscreteSensorType.ReagentDrawerClosed, true, EnumDiscreteSensorType.ReagentDrawerLocked, true, LockState.ClosedLocked)]
    [InlineData(DrawerType.RinseWaste, LockState.Open, EnumDiscreteSensorType.RinseWasteDrawerClosed, true, null, null, LockState.ClosedUnlocked)]
    [InlineData(DrawerType.SolidWaste, LockState.Open, EnumDiscreteSensorType.SolidWasteDrawerClosed, true, EnumDiscreteSensorType.SolidWasteDrawerLocked, true, LockState.ClosedLocked)]
    public async Task Handle_DrawerLockStateIsUpdatedFromSensors(
        DrawerType drawerType,
        LockState? initialLockState,
        EnumDiscreteSensorType? closedSensorType,
        bool? closedSensorValue,
        EnumDiscreteSensorType? lockedSensorType,
        bool? lockedSensorValue,
        LockState? expectedLockState,
        bool throwsException = false)
    {
        // Arrange
        var lockStates = Domain.Instrument.CreateDefaultDrawerLockStates();
        var instrument = new Domain.Instrument { Id = 1, SerialNumber = _instrumentNumber, DrawersLockState = lockStates };
        var sut = CreateSut(instrument);
        var message = new SensorInformation();
        if (initialLockState.HasValue)
        {
            var index = lockStates.FindIndex(x => x.Drawer == drawerType);
            lockStates.RemoveAt(index);
            lockStates.Add(new DrawerLockState(drawerType, initialLockState.Value, false));
        }

        if (closedSensorType.HasValue)
        {
            message.DiscreteSensors.Add(new DiscreteSensor { Type = closedSensorType.Value, Value = closedSensorValue!.Value ? EnumDiscreteSensorValue.Yes : EnumDiscreteSensorValue.No });
        }

        if (lockedSensorType.HasValue)
        {
            message.DiscreteSensors.Add(new DiscreteSensor { Type = lockedSensorType.Value, Value = lockedSensorValue!.Value ? EnumDiscreteSensorValue.Yes : EnumDiscreteSensorValue.No });
        }

        // Act
        try
        {
            await sut.Handle(new ProcessSensorInformationCommand(_instrumentNumber, message), TestContext.Current.CancellationToken);
        }
        catch (Exception e)
        {
            if (throwsException && e is ValidationException)
            {
                return;
            }
            throw;
        }

        // Assert
        instrument.DrawersLockState.Should().NotBeNullOrEmpty();
        var drawerLockState = instrument.DrawersLockState.Single(state => state.Drawer == drawerType);
        drawerLockState.LockState.Should().Be(expectedLockState);
    }

    [Theory]
    [InlineData(null, null, null, LockState.ClosedLocked)]
    [InlineData(LockState.ClosedLocked, true, true, LockState.ClosedLocked)]
    [InlineData(LockState.ClosedLocked, true, false, LockState.ClosedUnlocked)]
    [InlineData(LockState.ClosedLocked, false, false, LockState.Open)]
    [InlineData(LockState.ClosedLocked, false, true, null, true)]
    [InlineData(LockState.ClosedUnlocked, true, true, LockState.ClosedLocked)]
    [InlineData(LockState.ClosedUnlocked, true, false, LockState.ClosedUnlocked)]
    [InlineData(LockState.ClosedUnlocked, false, false, LockState.Open)]
    [InlineData(LockState.ClosedUnlocked, false, true, null, true)]
    [InlineData(LockState.Open, true, true, LockState.ClosedLocked)]
    [InlineData(LockState.Open, true, false, LockState.ClosedUnlocked)]
    [InlineData(LockState.Open, false, false, LockState.Open)]
    [InlineData(LockState.Open, false, true, null, true)]
    [InlineData(LockState.ClosedLocked, true, null, LockState.ClosedLocked)]
    [InlineData(LockState.ClosedUnlocked, true, null, LockState.ClosedUnlocked)]
    [InlineData(LockState.Open, true, null, LockState.ClosedUnlocked)]
    [InlineData(LockState.ClosedLocked, false, null, null, true)]
    [InlineData(LockState.ClosedUnlocked, false, null, LockState.Open)]
    [InlineData(LockState.Open, false, null, LockState.Open)]
    [InlineData(LockState.ClosedLocked, null, true, LockState.ClosedLocked)]
    [InlineData(LockState.ClosedUnlocked, null, true, LockState.ClosedLocked)]
    [InlineData(LockState.Open, null, true, null, true)]
    [InlineData(LockState.ClosedLocked, null, false, LockState.ClosedUnlocked)]
    [InlineData(LockState.ClosedUnlocked, null, false, LockState.ClosedUnlocked)]
    [InlineData(LockState.Open, null, false, LockState.Open)]
    public async Task Handle_HoodLockStateIsUpdatedFromSensors(
        LockState? initialLockState,
        bool? closedSensorValue,
        bool? lockedSensorValue,
        LockState? expectedLockState,
        bool throwsException = false)
    {
        // Arrange
        var instrument = new Domain.Instrument
        {
            Id = 1,
            SerialNumber = _instrumentNumber,
            DrawersLockState = Domain.Instrument.CreateDefaultDrawerLockStates(),
        };

        if (initialLockState != null)
        {
            instrument.HoodLockState = new Domain.HoodLockState(initialLockState.Value, false);
        }

        var sut = CreateSut(instrument);
        var message = new SensorInformation();
        if (closedSensorValue.HasValue)
        {
            message.DiscreteSensors.Add(new DiscreteSensor { Type = EnumDiscreteSensorType.HoodClosed, Value = closedSensorValue.Value ? EnumDiscreteSensorValue.Yes : EnumDiscreteSensorValue.No });
        }

        if (lockedSensorValue.HasValue)
        {
            message.DiscreteSensors.Add(new DiscreteSensor { Type = EnumDiscreteSensorType.HoodLocked, Value = lockedSensorValue.Value ? EnumDiscreteSensorValue.Yes : EnumDiscreteSensorValue.No });
        }

        // Act
        try
        {
            await sut.Handle(new ProcessSensorInformationCommand(_instrumentNumber, message), TestContext.Current.CancellationToken);
        }
        catch (Exception e)
        {
            if (throwsException && e is ValidationException)
            {
                return;
            }
            throw;
        }

        // Assert
        instrument.HoodLockState.LockState.Should().Be(expectedLockState);
    }

    [Fact]
    public async Task Handle_HoodLockStateChanges_PublishesHoodLockStateChanged()
    {
        // Arrange
        var instrument = new Domain.Instrument
        {
            Id = 1,
            SerialNumber = _instrumentNumber,
        };
        var mediator = new Mock<IMediator>();
        var message = new SensorInformation
        {
            DiscreteSensors =
            {
                new DiscreteSensor { Type = EnumDiscreteSensorType.HoodClosed, Value = EnumDiscreteSensorValue.No },
                new DiscreteSensor { Type = EnumDiscreteSensorType.HoodLocked, Value = EnumDiscreteSensorValue.No },
            },
        };

        // Act
        await CreateSut(instrument, mediator: mediator)
            .Handle(new ProcessSensorInformationCommand(_instrumentNumber, message), TestContext.Current.CancellationToken);

        // Assert
        mediator.Verify(value => value.Publish(
            It.Is<HoodLockStateChanged>(notification =>
                notification.InstrumentNumber == instrument.SerialNumber &&
                notification.PreviousLockState.LockState == LockState.ClosedLocked &&
                notification.CurrentLockState.LockState == LockState.Open &&
                !notification.CurrentLockState.IsUnlockInProgress),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DrawerLockStateChanges_PublishesDrawerLockStateChanged()
    {
        // Arrange
        var instrument = new Domain.Instrument
        {
            Id = 1,
            SerialNumber = _instrumentNumber,
            DrawersLockState = Domain.Instrument.CreateDefaultDrawerLockStates(),
        };
        var mediator = new Mock<IMediator>();
        var message = new SensorInformation
        {
            DiscreteSensors =
            {
                new DiscreteSensor { Type = EnumDiscreteSensorType.SampleDrawer1Closed, Value = EnumDiscreteSensorValue.No },
                new DiscreteSensor { Type = EnumDiscreteSensorType.SampleDrawer1Locked, Value = EnumDiscreteSensorValue.No },
                new DiscreteSensor { Type = EnumDiscreteSensorType.SampleDrawer2Closed, Value = EnumDiscreteSensorValue.No },
                new DiscreteSensor { Type = EnumDiscreteSensorType.SampleDrawer2Locked, Value = EnumDiscreteSensorValue.No },
            },
        };

        // Act
        await CreateSut(instrument, mediator: mediator).Handle(new ProcessSensorInformationCommand(_instrumentNumber, message), TestContext.Current.CancellationToken);

        // Assert
        mediator.Verify(
            value => value.Publish(
                It.Is<DrawerLockStateChanged>(notification =>
                    notification.InstrumentNumber == _instrumentNumber &&
                    notification.PreviousState!.Single(state => state.Drawer == DrawerType.Sample1).LockState == LockState.ClosedLocked &&
                    notification.CurrentState.Single(state => state.Drawer == DrawerType.Sample1).LockState == LockState.Open &&
                    notification.PreviousState!.Single(state => state.Drawer == DrawerType.Sample2).LockState == LockState.ClosedLocked &&
                    notification.CurrentState.Single(state => state.Drawer == DrawerType.Sample2).LockState == LockState.Open),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EnvironmentIsUpdatedFromSensor()
    {
        // Arrange
        var instrument = new Domain.Instrument
        {
            Id = 1,
            SerialNumber = _instrumentNumber,
        };
        var sensorType = EnumContinuousSensorType.BackStorageCarrierAirLowerElevatorRelativeHumidity;
        var message = new SensorInformation
        {
            ContinuousSensors =
            {
                new ContinuousSensor
                {
                    Type = sensorType,
                    Value = 1,
                    LowerLimit = 2,
                    UpperLimit = 3,
                    OutsideLimit = true,
                },
            },
        };

        // Act
        await CreateSut(instrument).Handle(new ProcessSensorInformationCommand(_instrumentNumber, message), TestContext.Current.CancellationToken);

        // Assert
        var existingSensor = instrument.EnvironmentSensors.First(x => x.Type == sensorType);
        existingSensor.Value.Should().Be(1);
        existingSensor.LowerLimit.Should().Be(2);
        existingSensor.UpperLimit.Should().Be(3);
        existingSensor.OutsideLimit.Should().Be(true);
    }

    [Theory]
    [InlineData(false, false, false, false, false)]
    [InlineData(true, false, false, false, true)]
    [InlineData(false, true, false, false, true)]
    [InlineData(false, false, true, false, true)]
    [InlineData(false, false, false, true, true)]
    public async Task Handle_ChangingEnvironmentPublishesInstrumentEnvironmentChanged(bool changeValue, bool changeLowerLimit, bool changeUpperLimit, bool changeOutsideLimit, bool publishEvent)
    {
        // Arrange
        var instrument = new Domain.Instrument
        {
            Id = 1,
            SerialNumber = _instrumentNumber,
            DrawersLockState = Domain.Instrument.CreateDefaultDrawerLockStates(),
        };
        var changedSensorType = EnumContinuousSensorType.CoreErwLidDevIncubationAirTemperature;
        var unchangedSensorType = EnumContinuousSensorType.BackStorageCarrierAirLowerElevatorRelativeHumidity;
        var changedSensor = instrument.EnvironmentSensors.Single(sensor => sensor.Type == changedSensorType);
        changedSensor.Value = 1;
        changedSensor.LowerLimit = 2;
        changedSensor.UpperLimit = 3;
        changedSensor.OutsideLimit = false;
        var unchangedSensor = instrument.EnvironmentSensors.Single(sensor => sensor.Type == unchangedSensorType);
        unchangedSensor.Value = 4;
        unchangedSensor.LowerLimit = 5;
        unchangedSensor.UpperLimit = 6;
        unchangedSensor.OutsideLimit = false;
        var mediator = new Mock<IMediator>();
        var message = new SensorInformation
        {
            ContinuousSensors =
            {
                new ContinuousSensor
                {
                    Type = changedSensorType,
                    Value = changeValue ? 10 : changedSensor.Value,
                    LowerLimit = changeLowerLimit ? 20 : changedSensor.LowerLimit,
                    UpperLimit = changeUpperLimit ? 30 : changedSensor.UpperLimit,
                    OutsideLimit = changeOutsideLimit,
                },
                new ContinuousSensor
                {
                    Type = unchangedSensorType,
                    Value = unchangedSensor.Value,
                    LowerLimit = unchangedSensor.LowerLimit,
                    UpperLimit = unchangedSensor.UpperLimit,
                    OutsideLimit = unchangedSensor.OutsideLimit,
                },
            },
        };

        // Act
        await CreateSut(instrument, mediator: mediator)
            .Handle(new ProcessSensorInformationCommand(_instrumentNumber, message), TestContext.Current.CancellationToken);

        // Assert
        mediator.Verify(
            value => value.Publish(
                It.Is<InstrumentEnvironmentChanged>(notification => notification.InstrumentId == instrument.Id),
                It.IsAny<CancellationToken>()),
            publishEvent ? Times.Once() : Times.Never());
    }

    [Theory]
    [InlineData(EnumContinuousSensorType.CoreErwLidDevIncubationAirTemperature, TaskCardType.TemperatureOutOfRange, true)]
    [InlineData(EnumContinuousSensorType.CoreErwLidDevIncubationAirTemperature, TaskCardType.TemperatureOutOfRange, false)]
    [InlineData(EnumContinuousSensorType.SampleDrawerAreaHumidity, TaskCardType.HumidityOutOfRange, true)]
    [InlineData(EnumContinuousSensorType.SampleDrawerAreaHumidity, TaskCardType.HumidityOutOfRange, false)]
    public async Task Handle_EnvironmentSensorOutsideLimit_UpdatesTaskCard(
        EnumContinuousSensorType sensorType,
        TaskCardType taskCardType,
        bool outsideLimit)
    {
        // Arrange
        var instrument = new Domain.Instrument
        {
            Id = 1,
            SerialNumber = _instrumentNumber,
            DrawersLockState = Domain.Instrument.CreateDefaultDrawerLockStates(),
        };
        var mediator = new Mock<IMediator>();
        var message = new SensorInformation
        {
            ContinuousSensors =
            {
                new ContinuousSensor
                {
                    Type = sensorType,
                    Value = 20,
                    LowerLimit = 18,
                    UpperLimit = 22,
                    OutsideLimit = outsideLimit,
                },
            },
        };

        // Act
        await CreateSut(instrument, mediator: mediator)
            .Handle(new ProcessSensorInformationCommand(_instrumentNumber, message), TestContext.Current.CancellationToken);

        // Assert
        if (outsideLimit)
        {
            mediator.Verify(
                value => value.Send(
                    It.Is<TaskCardCommand>(command =>
                        command.CardType == taskCardType &&
                        command.Rules.Single().CardState == TaskCardState.Error &&
                        command.InstrumentNumber == _instrumentNumber),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        else
        {
            mediator.Verify(
                value => value.Send(
                    It.Is<RemoveTaskCardCommand>(command => command.CardType == taskCardType && command.InstrumentNumber == _instrumentNumber),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    [Theory]
    [InlineData(DrawerType.Sample1, TaskCardType.SampleDrawerOpen)]
    [InlineData(DrawerType.Sample2, TaskCardType.SampleDrawerOpen)]
    [InlineData(DrawerType.Reagent, TaskCardType.ReagentDrawerOpen)]
    [InlineData(DrawerType.BottledReagent, TaskCardType.BottleDrawerOpen)]
    [InlineData(DrawerType.RinseWaste, TaskCardType.LiquidDrawerOpen)]
    [InlineData(DrawerType.SolidWaste, TaskCardType.SolidWasteDrawerOpen)]
    public async Task Handle_DrawerOpen_SendsDrawerTaskCard(DrawerType drawerType, TaskCardType taskCardType)
    {
        // Arrange
        var now = new DateTimeOffset(2026, 3, 18, 12, 0, 0, TimeSpan.Zero);
        var drawerStates = Domain.Instrument.CreateDefaultDrawerLockStates();
        var drawerIndex = drawerStates.FindIndex(drawer => drawer.Drawer == drawerType);
        drawerStates[drawerIndex] = new DrawerLockState(drawerType, LockState.Open, false, now.AddMinutes(-1));
        var instrument = new Domain.Instrument
        {
            Id = 1,
            SerialNumber = _instrumentNumber,
            DrawersLockState = drawerStates,
        };
        var mediator = new Mock<IMediator>();
        var dateTime = new Mock<IDateTime>();
        dateTime.Setup(value => value.Now).Returns(now);

        // Act
        await CreateSut(instrument, dateTime.Object, mediator: mediator)
            .Handle(new ProcessSensorInformationCommand(_instrumentNumber, new SensorInformation()), TestContext.Current.CancellationToken);

        // Assert
        mediator.Verify(
            value => value.Send(
                It.Is<TaskCardCommand>(command => command.CardType == taskCardType && command.InstrumentNumber == _instrumentNumber),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(TaskCardType.SampleDrawerOpen)]
    [InlineData(TaskCardType.ReagentDrawerOpen)]
    [InlineData(TaskCardType.BottleDrawerOpen)]
    [InlineData(TaskCardType.LiquidDrawerOpen)]
    [InlineData(TaskCardType.SolidWasteDrawerOpen)]
    public async Task Handle_DrawerClosed_RemovesDrawerTaskCard(TaskCardType taskCardType)
    {
        // Arrange
        var instrument = new Domain.Instrument
        {
            Id = 1,
            SerialNumber = _instrumentNumber,
            DrawersLockState = Domain.Instrument.CreateDefaultDrawerLockStates(),
        };
        var mediator = new Mock<IMediator>();

        // Act
        await CreateSut(instrument, mediator: mediator)
            .Handle(new ProcessSensorInformationCommand(_instrumentNumber, new SensorInformation()), TestContext.Current.CancellationToken);

        // Assert
        mediator.Verify(
            value => value.Send(
                It.Is<RemoveTaskCardCommand>(command => command.CardType == taskCardType && command.InstrumentNumber == _instrumentNumber),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(LockState.ClosedLocked, true, true, true, true)]
    [InlineData(LockState.ClosedLocked, false, true, false, false)]
    [InlineData(LockState.ClosedUnlocked, true, true, false, false)]
    [InlineData(LockState.Open, true, false, false, false)]
    public async Task Handle_DrawerUnlockInProgressIsUpdatedFromSensors(
        LockState initialLockState,
        bool initialIsUnlockInProgress,
        bool closedSensorValue,
        bool lockedSensorValue,
        bool expectedIsUnlockInProgress)
    {
        // Arrange
        var drawerStates = Domain.Instrument.CreateDefaultDrawerLockStates();
        var sampleDrawerIndex = drawerStates.FindIndex(state => state.Drawer == DrawerType.Sample1);
        drawerStates[sampleDrawerIndex] = new DrawerLockState(DrawerType.Sample1, initialLockState, initialIsUnlockInProgress);
        var instrument = new Domain.Instrument { Id = 1, SerialNumber = _instrumentNumber, DrawersLockState = drawerStates };
        var message = new SensorInformation
        {
            DiscreteSensors =
            {
                new DiscreteSensor { Type = EnumDiscreteSensorType.SampleDrawer1Closed, Value = closedSensorValue ? EnumDiscreteSensorValue.Yes : EnumDiscreteSensorValue.No },
                new DiscreteSensor { Type = EnumDiscreteSensorType.SampleDrawer1Locked, Value = lockedSensorValue ? EnumDiscreteSensorValue.Yes : EnumDiscreteSensorValue.No },
            },
        };

        // Act
        await CreateSut(instrument).Handle(new ProcessSensorInformationCommand(_instrumentNumber, message), TestContext.Current.CancellationToken);

        // Assert
        instrument.DrawersLockState.Single(state => state.Drawer == DrawerType.Sample1).IsUnlockInProgress.Should().Be(expectedIsUnlockInProgress);
    }

    [Theory]
    [InlineData(LockState.ClosedLocked, false, false, false)]
    [InlineData(LockState.Open, false, false, true)]
    [InlineData(LockState.Open, true, true, false)]
    public async Task Handle_DrawerOpenedAtIsUpdatedFromSensors(
        LockState initialLockState,
        bool closedSensorValue,
        bool lockedSensorValue,
        bool retainsOpenedAt)
    {
        // Arrange
        var now = new DateTimeOffset(2026, 3, 18, 12, 0, 0, TimeSpan.Zero);
        var previouslyOpenedAt = now.AddMinutes(-1);
        var drawerStates = Domain.Instrument.CreateDefaultDrawerLockStates();
        var sampleDrawerIndex = drawerStates.FindIndex(state => state.Drawer == DrawerType.Sample1);
        drawerStates[sampleDrawerIndex] = new DrawerLockState(DrawerType.Sample1, initialLockState, false, previouslyOpenedAt);
        var instrument = new Domain.Instrument { Id = 1, SerialNumber = _instrumentNumber, DrawersLockState = drawerStates };
        var dateTime = new Mock<IDateTime>();
        dateTime.Setup(x => x.Now).Returns(now);
        var message = new SensorInformation
        {
            DiscreteSensors =
            {
                new DiscreteSensor { Type = EnumDiscreteSensorType.SampleDrawer1Closed, Value = closedSensorValue ? EnumDiscreteSensorValue.Yes : EnumDiscreteSensorValue.No },
                new DiscreteSensor { Type = EnumDiscreteSensorType.SampleDrawer1Locked, Value = lockedSensorValue ? EnumDiscreteSensorValue.Yes : EnumDiscreteSensorValue.No },
            },
        };

        // Act
        await CreateSut(instrument, dateTime.Object).Handle(new ProcessSensorInformationCommand(_instrumentNumber, message), TestContext.Current.CancellationToken);

        // Assert
        var drawerState = instrument.DrawersLockState.Single(state => state.Drawer == DrawerType.Sample1);
        drawerState.OpenedAt.Should().Be(retainsOpenedAt ? previouslyOpenedAt : initialLockState == LockState.Open ? null : now);
    }

    private static ProcessSensorInformationCommandHandler CreateSut(
        Domain.Instrument instrument,
        IDateTime? dateTime = null,
        Mock<IMediator>? mediator = null)
    {
        var instrumentManager = new Mock<IInstrumentManager>();
        instrumentManager.Setup(manager => manager.GetInstrument(_instrumentNumber)).Returns(instrument);

        mediator ??= new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mediator.Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        dateTime ??= new Mock<IDateTime>().Object;
        return new ProcessSensorInformationCommandHandler(
            instrumentManager.Object,
            mediator.Object,
            dateTime);
    }
}
