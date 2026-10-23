// ReSharper disable InconsistentNaming
namespace Pharos.Server.Lis.Astm.Application.Common;

internal static class AstmControlSymbols
{
    public const byte SOH = 0x1;

    /// <summary>
    /// Start of Text transmission control character.
    /// </summary>
    public const byte STX = 0x2;

    /// <summary>
    /// Character used to indicate the end of an end frame. An option in the system
    /// will make it possible to use only ETX frame for messages.
    /// </summary>
    public const byte ETX = 0x3;

    /// <summary>
    /// End of transmission.
    /// </summary>
    public const byte EOT = 0x4;

    /// <summary>
    /// The request of a new transmission.
    /// </summary>
    public const byte ENQ = 0x5;

    /// <summary>
    /// The message has been acknowledged.
    /// </summary>
    public const byte ACK = 0x6;

    /// <summary>
    /// The [LF] character is used as the last character of a frame. The[LF] character may not appear in the message text.
    /// </summary>
    public const byte LF = 0x0A;

    /// <summary>
    /// Character used to end a record. The[CR] character may not appear in the message text.
    /// </summary>
    public const byte CR = 0x0D;
    public const char RecordDelimiter = (char)CR;

    public const byte DLE = 0x10;
    public const byte DC1 = 0x11;
    public const byte DC2 = 0x12;
    public const byte DC3 = 0x13;
    public const byte DC4 = 0x14;

    /// <summary>
    /// The message has not been acknowledged.
    /// </summary>
    public const byte NAK = 0x15;

    public const byte SYN = 0x16;

    /// <summary>
    /// Character used to indicate end of an intermediate frame used
    /// when the Message to send is more than 240 characters long,
    /// in that case the message are divided into more than one frames.
    /// The last frame will end with an [ETX] character.
    /// </summary>
    public const byte ETB = 0x17;

    public static bool ContainsControlSymbols(byte[] data)
    {
        return data.Intersect(_controlSymbols).Any();
    }

    public static bool IsFrameTerminator(int b)
    {
        return b is ETX or ETB;
    }

    public static bool IsControlSymbol(byte data)
    {
        return Array.IndexOf(_controlSymbols, data) >= 0;
    }


    private static readonly byte[] _controlSymbols =
    {
        SOH,
        STX,
        ETX,
        EOT,
        ENQ,
        ACK,
        LF,
        DLE,
        DC1,
        DC2,
        DC3,
        DC4,
        NAK,
        SYN,
        ETB
    };
}
