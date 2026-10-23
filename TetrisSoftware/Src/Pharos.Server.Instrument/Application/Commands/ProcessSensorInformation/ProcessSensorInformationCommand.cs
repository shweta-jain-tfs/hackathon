using FluentValidation;
using Google.Protobuf.Collections;
using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Domain;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Commands.ProcessSensorInformation;

internal record ProcessSensorInformationCommand(string InstrumentNumber, SensorInformation Message) : IRequest;

internal class ProcessSensorInformationCommandHandler : IRequestHandler<ProcessSensorInformationCommand>
{
    private readonly IInstrumentManager _instrumentManager;
    private readonly IMediator _mediator;
    private readonly IDateTime _dateTime;

    public ProcessSensorInformationCommandHandler(IInstrumentManager instrumentManager, IMediator mediator, IDateTime dateTime)
    {
        _instrumentManager = instrumentManager;
        _mediator = mediator;
        _dateTime = dateTime;
    }

    public async Task Handle(ProcessSensorInformationCommand request, CancellationToken cancellationToken)
    {
        var instrument = (Domain.Instrument)_instrumentManager.GetInstrument(request.InstrumentNumber);

        await ProcessHoodSensor(instrument, request.Message.DiscreteSensors, cancellationToken);

        await ProcessDrawerSensors(instrument, request.Message.DiscreteSensors, cancellationToken);

        await ProcessEnvironmentSensors(instrument, request.Message.ContinuousSensors, cancellationToken);
    }

    private async Task ProcessHoodSensor(Domain.Instrument instrument, RepeatedField<DiscreteSensor> sensors, CancellationToken cancellationToken)
    {
        var previousHoodLockState = instrument.HoodLockState;

        var sensorValue = sensors.FirstOrDefault(s => s.Type == EnumDiscreteSensorType.HoodClosed)?.Value;
        if (sensorValue == null)
        {
            sensorValue = previousHoodLockState.LockState switch
            {
                LockState.Open => EnumDiscreteSensorValue.No,
                _ => EnumDiscreteSensorValue.Yes
            };
        }

        var closed = sensorValue.Value == EnumDiscreteSensorValue.Yes;

        sensorValue = sensors.FirstOrDefault(s => s.Type == EnumDiscreteSensorType.HoodLocked)?.Value;
        if (sensorValue == null)
        {
            sensorValue = previousHoodLockState.LockState switch
            {
                LockState.ClosedLocked => EnumDiscreteSensorValue.Yes,
                _ => EnumDiscreteSensorValue.No
            };
        }
        
        var locked = sensorValue.Value == EnumDiscreteSensorValue.Yes;
        var currentLockState = DetermineHoodLockState(closed, locked);
        instrument.HoodLockState = new HoodLockState(currentLockState, currentLockState == LockState.ClosedLocked && previousHoodLockState.IsUnlockInProgress);

        if (previousHoodLockState.LockState != instrument.HoodLockState.LockState || previousHoodLockState.IsUnlockInProgress != instrument.HoodLockState.IsUnlockInProgress)
        {
            await _mediator.Publish(new HoodLockStateChanged(instrument.SerialNumber, previousHoodLockState, instrument.HoodLockState), cancellationToken);
        }
    }

    private async Task ProcessDrawerSensors(Domain.Instrument instrument, RepeatedField<DiscreteSensor> newSensors, CancellationToken cancellationToken)
    {
        var oldDrawerState = instrument.DrawersLockState;
        var newDrawersState = BuildDrawerLockState(instrument, newSensors);
        instrument.DrawersLockState = newDrawersState;

        await HandleTaskCards(instrument, cancellationToken);

        for (var i = 0; i < newDrawersState.Count; i++)
        {
            if (oldDrawerState[i].LockState != newDrawersState[i].LockState || oldDrawerState[i].IsUnlockInProgress != newDrawersState[i].IsUnlockInProgress)
            {
                await _mediator.Publish(new DrawerLockStateChanged(instrument.SerialNumber, oldDrawerState, newDrawersState), cancellationToken);
                break;
            }
        }
    }

    private async Task HandleTaskCards(Domain.Instrument instrument, CancellationToken cancellationToken)
    {
        await HandleSampleDrawersTaskCard(instrument, cancellationToken);
        await HandleDrawerTaskCard(TaskCardType.ReagentDrawerOpen, instrument, DrawerType.Reagent, cancellationToken);
        await HandleDrawerTaskCard(TaskCardType.BottleDrawerOpen, instrument, DrawerType.BottledReagent, cancellationToken);
        await HandleDrawerTaskCard(TaskCardType.LiquidDrawerOpen, instrument, DrawerType.RinseWaste, cancellationToken);
        await HandleDrawerTaskCard(TaskCardType.SolidWasteDrawerOpen, instrument, DrawerType.SolidWaste, cancellationToken);
    }

    private async Task HandleSampleDrawersTaskCard(Domain.Instrument instrument, CancellationToken cancellationToken)
    {
        var sampleDrawer1 = instrument.DrawersLockState.FirstOrDefault(x => x.Drawer == DrawerType.Sample1);
        var sampleDrawer2 = instrument.DrawersLockState.FirstOrDefault(x => x.Drawer == DrawerType.Sample2);

        if (sampleDrawer1 == null || sampleDrawer2 == null ||
            (sampleDrawer1.LockState == LockState.ClosedLocked && sampleDrawer2.LockState == LockState.ClosedLocked))
        {
            await _mediator.Send(new RemoveTaskCardCommand(TaskCardType.SampleDrawerOpen, instrument.SerialNumber), cancellationToken);
        }
        else
        {
            var alertTrigger = GetSampleDrawerAlertTrigger(sampleDrawer1, sampleDrawer2);

            if (alertTrigger > 0)
            {
                await _mediator.Send(new TaskCardCommand(TaskCardType.SampleDrawerOpen,
                                        [
                                            new EscalationRule(TaskCardState.Hidden, TimeSpan.Zero),
                                            new EscalationRule(TaskCardState.Warning, TimeSpan.FromMinutes(alertTrigger))
                                        ], instrument.SerialNumber), cancellationToken);
            }
        }
    }

    private double GetSampleDrawerAlertTrigger(DrawerLockState sample1, DrawerLockState sample2)
    {
        var drawer1AlertTrigger = CalculateDrawerAlertTrigger(sample1);
        var drawer2AlertTrigger = CalculateDrawerAlertTrigger(sample2);

        // DrawerAlertTrigger zero indicates the drawer is not open
        // Negative indicates the drawer is open > 5 minutes
        // Positive indicates the drawer is open < 5 minutes

        if (drawer1AlertTrigger > 0 && drawer2AlertTrigger > 0)
        {
            return Math.Min(drawer1AlertTrigger, drawer2AlertTrigger);
        }

        if (drawer1AlertTrigger > 0 && drawer2AlertTrigger == 0)
        {
            return drawer1AlertTrigger;
        }

        if (drawer2AlertTrigger > 0 && drawer1AlertTrigger == 0)
        {
            return drawer2AlertTrigger;
        }

        return 0.0;
    }


    private async Task HandleDrawerTaskCard(TaskCardType taskCardType, Domain.Instrument instrument, DrawerType drawerType, CancellationToken cancellationToken)
    {
        var drawer = instrument.DrawersLockState.FirstOrDefault(x => x.Drawer == drawerType);

        if (drawer is not { LockState: LockState.Open })
        {
            await _mediator.Send(new RemoveTaskCardCommand(taskCardType, instrument.SerialNumber), cancellationToken);
        }
        else
        {
            var alertTrigger = CalculateDrawerAlertTrigger(drawer);

            if (alertTrigger > 0)
            {
                await _mediator.Send(new TaskCardCommand(taskCardType,
                [
                    new EscalationRule(TaskCardState.Hidden, TimeSpan.Zero),
                    new EscalationRule(TaskCardState.Warning, TimeSpan.FromMinutes(alertTrigger))
                ], instrument.SerialNumber), cancellationToken);
            }
        }
    }


    private double CalculateDrawerAlertTrigger(DrawerLockState drawer)
    {
        var trigger = 5;
        if (drawer.LockState == LockState.Open && drawer.OpenedAt.HasValue)
        {
            var openDuration = (_dateTime.Now - drawer.OpenedAt.Value).TotalMinutes;
            return trigger - openDuration;
        }

        return 0.0;
    }

    private DrawerLockStateList BuildDrawerLockState(Domain.Instrument instrument, RepeatedField<DiscreteSensor> sensors)
    {
        var result = new DrawerLockStateList();
        var previousState = instrument.DrawersLockState;

        foreach (var drawer in Enum.GetValues<DrawerType>())
        {
            var lockState = drawer switch
            {
                DrawerType.Sample1 => DetermineLockState(drawer, previousState, sensors, EnumDiscreteSensorType.SampleDrawer1Closed, EnumDiscreteSensorType.SampleDrawer1Locked),
                DrawerType.Sample2 => DetermineLockState(drawer, previousState, sensors, EnumDiscreteSensorType.SampleDrawer2Closed, EnumDiscreteSensorType.SampleDrawer2Locked),
                DrawerType.Reagent => DetermineLockState(drawer, previousState, sensors, EnumDiscreteSensorType.ReagentDrawerClosed, EnumDiscreteSensorType.ReagentDrawerLocked),
                DrawerType.RinseWaste => DetermineLockState(drawer, previousState, sensors, EnumDiscreteSensorType.RinseWasteDrawerClosed, null),
                DrawerType.BottledReagent => DetermineLockState(drawer, previousState, sensors, EnumDiscreteSensorType.BottledReagentDrawerClosed, null),
                DrawerType.SolidWaste => DetermineLockState(drawer, previousState, sensors, EnumDiscreteSensorType.SolidWasteDrawerClosed, EnumDiscreteSensorType.SolidWasteDrawerLocked),
                _ => throw new InvalidOperationException("Invalid drawer type.")
            };

            var isUnlockInProgress = lockState == LockState.ClosedLocked && previousState.First(x => x.Drawer == drawer).IsUnlockInProgress;

            var openedAt = GetDrawerOpenedAt(lockState, drawer, previousState);

            result.Add(new DrawerLockState(drawer, lockState, isUnlockInProgress, openedAt));
        }

        return result;
    }

    private async Task ProcessEnvironmentSensors(Domain.Instrument instrument, RepeatedField<ContinuousSensor> sensors, CancellationToken cancellationToken)
    {
        var environmentSensorChanged = false;

        foreach (var sensor in sensors)
        {
            var existingSensor = instrument.EnvironmentSensors.Single(x => x.Type == sensor.Type);
            environmentSensorChanged |= UpdateSensorValue(existingSensor, sensor);
        }

        if (environmentSensorChanged)
        {
            await _mediator.Publish(new InstrumentEnvironmentChanged(instrument.Id), cancellationToken);
        }

        await HandleTemperatureTaskCard(instrument, cancellationToken);
        await HandleHumidityTaskCard(instrument, cancellationToken);
    }

    private async Task HandleHumidityTaskCard(Domain.Instrument instrument, CancellationToken cancellationToken)
    {
        var hasHumidityOutOfRange = instrument.EnvironmentSensors
            .Any(s => s.Unit == EnumUnit.RelativeHumidity && s.OutsideLimit);

        if (hasHumidityOutOfRange)
        {
            await _mediator.Send(
                new TaskCardCommand(TaskCardType.HumidityOutOfRange, TaskCardState.Error, instrument.SerialNumber),
                cancellationToken);
        }
        else
        {
            await _mediator.Send(
                new RemoveTaskCardCommand(TaskCardType.HumidityOutOfRange, instrument.SerialNumber),
                cancellationToken);
        }
    }

    private async Task HandleTemperatureTaskCard(Domain.Instrument instrument, CancellationToken cancellationToken)
    {
        var hasTemperatureOutOfRange = instrument.EnvironmentSensors
            .Any(s => s.Unit == EnumUnit.Celsius && s.OutsideLimit);

        if (hasTemperatureOutOfRange)
        {
            await _mediator.Send(
                new TaskCardCommand(TaskCardType.TemperatureOutOfRange, TaskCardState.Error, instrument.SerialNumber),
                cancellationToken);
        }
        else
        {
            await _mediator.Send(
                new RemoveTaskCardCommand(TaskCardType.TemperatureOutOfRange, instrument.SerialNumber),
                cancellationToken);
        }
    }

    private static bool UpdateSensorValue(ContinuousSensor existingSensor, ContinuousSensor newSensor)
    {
        if (HasSensorValuesChanged(existingSensor, newSensor))
        {
            existingSensor.Value = newSensor.Value;
            existingSensor.OutsideLimit = newSensor.OutsideLimit;
            existingSensor.UpperLimit = newSensor.UpperLimit;
            existingSensor.LowerLimit = newSensor.LowerLimit;
            return true;
        }

        return false;
    }

    private static bool HasSensorValuesChanged(ContinuousSensor existingSensor, ContinuousSensor newSensor)
    {
        return HasSensorValueChanged(existingSensor.Value, newSensor.Value) ||
               HasSensorValueChanged(existingSensor.UpperLimit, newSensor.UpperLimit) ||
               HasSensorValueChanged(existingSensor.LowerLimit, newSensor.LowerLimit) ||
               existingSensor.OutsideLimit != newSensor.OutsideLimit;
    }

    private static bool HasSensorValueChanged(double? existingValue, double newValue)
    {
        if (!existingValue.HasValue)
        {
            return true;
        }

        return Math.Abs(existingValue.Value - newValue) > 0.001;
    }

    private static LockState DetermineLockState(
        DrawerType drawer,
        IReadOnlyList<DrawerLockState> previousState,
        RepeatedField<DiscreteSensor> sensors,
        EnumDiscreteSensorType closedSensorType,
        EnumDiscreteSensorType? lockedSensorType)
    {
        var sensorValue = sensors.FirstOrDefault(s => s.Type == closedSensorType)?.Value;
        if (sensorValue == null)
        {
            sensorValue = previousState.First(x => x.Drawer == drawer).LockState switch
            {
                LockState.Open => EnumDiscreteSensorValue.No,
                _ => EnumDiscreteSensorValue.Yes
            };
        }
        var closed = sensorValue.Value == EnumDiscreteSensorValue.Yes;
        var locked = false;
        if (lockedSensorType != null)
        {
            sensorValue = sensors.FirstOrDefault(s => s.Type == lockedSensorType.Value)?.Value;
            if (sensorValue == null)
            {
                sensorValue = previousState.First(x => x.Drawer == drawer).LockState switch
                {
                    LockState.ClosedLocked => EnumDiscreteSensorValue.Yes,
                    _ => EnumDiscreteSensorValue.No
                };
            }
            locked = sensorValue.Value == EnumDiscreteSensorValue.Yes;
        }

        if (closed && locked)
        {
            return LockState.ClosedLocked;
        }

        if (closed)
        {
            return LockState.ClosedUnlocked;
        }

        if (!closed && !locked)
        {
            return LockState.Open;
        }

        throw new ValidationException($"Invalid sensor information. The {drawer} drawer cannot be open and locked at the same time.");
    }

    private DateTimeOffset? GetDrawerOpenedAt(LockState lockState, DrawerType drawer, IReadOnlyList<DrawerLockState>? previousState)
    {
        if (lockState != LockState.Open)
        {
            return null;
        }

        if (previousState == null || previousState.First(x => x.Drawer == drawer).LockState != lockState)
        {
            return _dateTime.Now;
        }

        return previousState.First(x => x.Drawer == drawer).OpenedAt;
    }

    private static LockState DetermineHoodLockState(bool closed, bool locked)
    {
        if (closed && locked)
        {
            return LockState.ClosedLocked;

        }
        else if (closed)
        {
            return LockState.ClosedUnlocked;
        }
        else if (!closed && !locked)
        {
            return LockState.Open;
        }
        else
        {
            throw new ValidationException($"Invalid sensor information. The instrument hood cannot be open and locked at the same time.");
        }
    }
}
