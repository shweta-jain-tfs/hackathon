using System.Net;
using Tetris.Protocol.Pharos.Pairing;

namespace Pharos.Server.Instrument.Application.Interfaces;

public interface IInstrumentPairingService
{
    /// <summary>
    /// The value indicating whether a new instrument can be paired with Pharos.
    /// </summary>
    bool PairingEnabled { get; }

    /// <summary>
    /// Enables the pairing mode.
    /// </summary>
    Task EnablePairing();

    /// <summary>
    /// Disables the pairing mode.
    /// </summary>
    Task DisablePairing();

    /// <summary>
    /// Starts an instrument pairing operation.
    /// </summary>
    Task<StartPairingResponse> StartPairing(StartPairingRequest request);

    /// <summary>
    /// Gets the serial number of the first instrument that is waiting for approval.
    /// </summary>
    /// <returns>An instrument serial number or null if no instruments are waiting for approval.</returns>
    string? GetPendingApproval();

    /// <summary>
    /// Returns the state of a pairing operation.
    /// </summary>
    /// <param name="pairingToken">The pairing operation ID.</param>
    EnumPairingState GetPairingState(string pairingToken);

    /// <summary>
    /// Handles response to a pairing request from the service engineer.
    /// </summary>
    Task HandleApproval(string instrumentNumber, bool approveParing);


    /// <summary>
    /// Finalizes an approved pairing request by checking the instrument private key ownership.
    /// </summary>
    Task<EnumPairingState> ConfirmPairing(ConfirmPairingRequest request, IPAddress instrumentIpAddress);
}
