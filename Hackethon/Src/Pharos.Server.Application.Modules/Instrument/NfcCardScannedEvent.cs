using MediatR;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// This event is published after a successful scan of an NFC card on the instrument.
/// </summary>
/// <param name="InstrumentNumber">The instrument serial number.</param>
/// <param name="ChipUid">
///  The NFC chip UID is a manufacturer-supplied, usually read-only, unique identifier for
///  the NFC chip used to uniquely identify an NFC tag.
/// </param>
public record NfcCardScannedEvent(string InstrumentNumber, string ChipUid) : INotification;
