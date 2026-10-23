namespace Pharos.Server.Instrument.Application.Commands.SendCmrTestOrder;

public class CmrTestOrderResultDto
{
    public CmrTestOrderStatus Status { get; set; }
}

public enum CmrTestOrderStatus
{
    Ok = 0,
    ParseError = 1,
    Invalid = 2,
    NotAllowed = 3,
    Unspecified = 4,
    InvalidState = 5,
}
