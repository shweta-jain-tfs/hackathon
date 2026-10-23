using MediatR;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// This event is published after an unsuccessful NFC card scan attempt on the instrument.
/// </summary>
/// <param name="InstrumentNumber">The instrument serial number.</param>
public record NfcCardErrorEvent(string InstrumentNumber) : INotification;
