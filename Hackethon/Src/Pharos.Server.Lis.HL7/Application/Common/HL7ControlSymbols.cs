namespace Pharos.Server.Lis.HL7.Application.Common;

internal static class HL7ControlSymbols
{
    public const byte StartBlock = 0x0B;
    public const byte EndBlock = 0x1C;
    public const byte CarriageReturn = 0x0D;
    public const char SegmentDelimiter = (char)CarriageReturn;

    public static bool ContainsControlSymbols(byte[] data)
    {
        return data.Intersect(_controlSymbols).Any();
    }

    private static readonly byte[] _controlSymbols =
    {
        StartBlock,
        EndBlock
    };
}
