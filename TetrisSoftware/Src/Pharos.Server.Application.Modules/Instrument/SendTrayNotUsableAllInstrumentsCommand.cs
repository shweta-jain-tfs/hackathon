using MediatR;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// Sends TrayNotUsable message to all connected instruments.
/// Used when a tray is scanned outside the instrument to prevent instruments from using it.
/// </summary>
public record SendTrayNotUsableAllInstrumentsCommand(string TrayBarcode) : IRequest;
