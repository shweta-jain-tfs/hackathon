using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Instrument;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Instrument.Application.Commands.UpdateSettings;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Events;

internal class InstrumentSettingsChangedEventHandler(
    ISender sender,
    IInstrumentManager instrumentManager,
    ICommunicationSessionManager sessionManager,
    ILogger<InstrumentSettingsChangedEventHandler> logger) : INotificationHandler<SettingsChangedEvent<InstrumentSettings>>, INotificationHandler<SettingsChangedEvent<TubeTypesSettings>>, INotificationHandler<SettingsChangedEvent<TubeBarcodesSettings>>
{
    public Task Handle(SettingsChangedEvent<InstrumentSettings> notification, CancellationToken cancellationToken)
    {
        return UpdateSettingsForChangedInstruments(notification.OldValue, notification.NewValue, cancellationToken);
    }

    public Task Handle(SettingsChangedEvent<TubeTypesSettings> notification, CancellationToken cancellationToken)
    {
        return UpdateSettingOnAllInstruments(cancellationToken);
    }

    public Task Handle(SettingsChangedEvent<TubeBarcodesSettings> notification, CancellationToken cancellationToken)
    {
        return UpdateSettingOnAllInstruments(cancellationToken);
    }

    private async Task UpdateSettingOnAllInstruments(CancellationToken cancellationToken)
    {
        foreach (var item in instrumentManager.GetInstruments())
        {
            await SendUpdateSettingsCommand(item.SerialNumber, cancellationToken);
        }
    }

    private async Task UpdateSettingsForChangedInstruments(InstrumentSettings oldValue, InstrumentSettings newValue, CancellationToken cancellationToken)
    {
        var changedInstrumentSettings = GetChangedInstrumentSettings(oldValue, newValue);
        if (changedInstrumentSettings == null)
        {
            return;
        }

        await SendUpdateSettingsCommand(changedInstrumentSettings.SerialNumber, cancellationToken);
    }

    private async Task SendUpdateSettingsCommand(string instrumentNumber, CancellationToken cancellationToken)
    {
        var session = sessionManager.GetSession(instrumentNumber);
        if (session is not { CompatibilityStatus: EnumCompatibilityStatus.FullyCompatible })
        {
            return;
        }

        try
        {
            await sender.Send(new UpdateSettingsCommand(instrumentNumber), cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send UpdateSettingsCommand to instrument {SerialNumber}", instrumentNumber);
        }
    }

    private static InstrumentSpecificSettings? GetChangedInstrumentSettings(InstrumentSettings oldValue, InstrumentSettings newValue)
    {
        foreach (var newEntry in newValue.Instruments)
        {
            var oldEntry = oldValue.Instruments.FirstOrDefault(i => IsSameInstrument(i, newEntry));
            if (oldEntry == null || HasInstrumentSpecificSettingsChanged(oldEntry, newEntry))
            {
                return newEntry;
            }
        }

        return null;
    }

    private static bool HasInstrumentSpecificSettingsChanged(InstrumentSpecificSettings oldValue, InstrumentSpecificSettings newValue)
    {
        return HasWasteChanged(oldValue.Waste, newValue.Waste)
            || HasRinseChanged(oldValue.Rinse, newValue.Rinse)
            || HasDrawerLockChanged(oldValue.DrawerLock, newValue.DrawerLock)
            || HasReagentsChanged(oldValue.Reagents, newValue.Reagents);
    }

    private static bool IsSameInstrument(InstrumentSpecificSettings oldEntry, InstrumentSpecificSettings newEntry)
    {
        if (!string.IsNullOrWhiteSpace(oldEntry.SerialNumber) || !string.IsNullOrWhiteSpace(newEntry.SerialNumber))
        {
            return string.Equals(oldEntry.SerialNumber, newEntry.SerialNumber, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool HasWasteChanged(Waste oldValue, Waste newValue)
    {
        return oldValue.MaximumLiquidWasteFillingVolume != newValue.MaximumLiquidWasteFillingVolume
            || oldValue.PipedOutWasteInstalled != newValue.PipedOutWasteInstalled
            || oldValue.PipedOutActive != newValue.PipedOutActive;
    }

    private static bool HasRinseChanged(Rinse oldValue, Rinse newValue)
    {
        return oldValue.PipedInRinseInstalled != newValue.PipedInRinseInstalled
            || oldValue.PipedInActive != newValue.PipedInActive;
    }

    private static bool HasDrawerLockChanged(DrawerLock oldValue, DrawerLock newValue)
    {
        return oldValue.AutoLockCompartmentTimeout != newValue.AutoLockCompartmentTimeout;
    }

    private static bool HasReagentsChanged(Reagents oldValue, Reagents newValue)
    {
        return oldValue.KeepExpiredReagentsOnBoard != newValue.KeepExpiredReagentsOnBoard
            || oldValue.KeepEmptyReagentsOnBoard != newValue.KeepEmptyReagentsOnBoard;
    }
}
