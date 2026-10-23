namespace Pharos.Server.Lis.HL7.Application.Common;

internal sealed class HL7Exception : Exception
{
    public HL7Exception()
       : base(string.Empty)
    {
    }

    public HL7Exception(string? message)
        : base(message)
    {
    }

    public HL7Exception(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
