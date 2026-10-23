using Tetris.Protocol.Pharos.Pairing;

namespace Pharos.Server.Instrument.Application.Services.Pairing;

internal sealed class PairingOperation(string token, byte[] instrumentCa, string instrumentSerial, byte[] pharosNonce, DateTimeOffset expiresAt)
{
    public string Token { get; } = token;
    public byte[] InstrumentCa { get; } = instrumentCa;
    public string InstrumentSerial { get; } = instrumentSerial;
    public byte[] PharosNonce { get; } = pharosNonce;
    public DateTimeOffset ExpiresAt { get; } = expiresAt;
    public EnumPairingState State { get; set; } = EnumPairingState.WaitingForApproval;
}