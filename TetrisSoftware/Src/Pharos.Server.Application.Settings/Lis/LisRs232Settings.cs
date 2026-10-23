using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

/// <summary>
/// RS-232 communication protocol settings for a LIS connection.
/// </summary>
public class LisRs232Settings
{
    /// <summary>
    /// Gets or sets the connection port number.
    /// </summary>
    [DisplayNameLocal("LisRs232Settings_Port")]
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the Baud rate setting.
    /// </summary>
    [DisplayNameLocal("LisRs232Settings_BaudRate")]
    public int BaudRate { get; set; }

    /// <summary>
    /// Gets or sets the data bits setting.
    /// </summary>
    [DisplayNameLocal("LisRs232Settings_DataBits")]
    public Rs232DataBits DataBits { get; set; } = Rs232DataBits.Bit7;

    /// <summary>
    /// Gets or sets the stop bits setting.
    /// </summary>
    [DisplayNameLocal("LisRs232Settings_StopBits")]
    public Rs232StopBits StopBits { get; set; }

    /// <summary>
    /// Gets or sets the parity setting.
    /// </summary>
    [DisplayNameLocal("LisRs232Settings_Parity")]
    public Rs232Parity Parity { get; set; }

    /// <summary>
    /// Gets or sets the handshake protocol setting.
    /// </summary>
    [DisplayNameLocal("LisRs232Settings_Handshake")]
    public Rs232Handshake Handshake { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether DTR (Data Terminal Ready) is enabled.
    /// </summary>
    [DisplayNameLocal("LisRs232Settings_EnableDtr")]
    public bool EnableDtr { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether RTS (Request To Send) is enabled.
    /// </summary>
    [DisplayNameLocal("LisRs232Settings_EnableRts")]
    public bool EnableRts { get; set; }
}

public enum Rs232DataBits
{
    [DisplayNameLocal("LisRs232Settings_DataBits7")]
    Bit7 = 7,

    [DisplayNameLocal("LisRs232Settings_DataBits8")]
    Bit8 = 8
}

public enum Rs232StopBits
{
    [DisplayNameLocal("LisRs232Settings_StopBitsNone")]
    None = 0,

    [DisplayNameLocal("LisRs232Settings_StopBitsOne")]
    One = 1,

    [DisplayNameLocal("LisRs232Settings_StopBitsTwo")]
    Two = 2,

    [DisplayNameLocal("LisRs232Settings_StopBitsOnePointFive")]
    OnePointFive = 3,
}

public enum Rs232Parity
{
    [DisplayNameLocal("LisRs232Settings_ParityNone")]
    None = 0,

    [DisplayNameLocal("LisRs232Settings_ParityOdd")]
    Odd = 1,

    [DisplayNameLocal("LisRs232Settings_ParityEven")]
    Even = 2,

    [DisplayNameLocal("LisRs232Settings_ParityMark")]
    Mark = 3,

    [DisplayNameLocal("LisRs232Settings_ParitySpace")]
    Space = 4,
}
public enum Rs232Handshake
{
    [DisplayNameLocal("LisRs232Settings_HandshakeNone")]
    None = 0,

    [DisplayNameLocal("LisRs232Settings_HandshakeXOnXOff")]
    XOnXOff = 1,

    [DisplayNameLocal("LisRs232Settings_HandshakeRequestToSend")]
    RequestToSend = 2,

    [DisplayNameLocal("LisRs232Settings_HandshakeRequestToSendXOnXOff")]
    RequestToSendXOnXOff = 3,
}