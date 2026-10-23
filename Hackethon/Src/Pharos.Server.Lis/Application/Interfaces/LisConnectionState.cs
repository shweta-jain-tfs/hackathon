namespace Pharos.Server.Lis.Application.Interfaces;

/// <summary>
/// Lists LIS connection states.
/// </summary>
public enum LisConnectionState
{
    /// <summary>
    /// The connection is closed.
    /// </summary>
    Closed,

    /// <summary>
    /// The LIS connection in the server mode is awaiting for a client.
    /// </summary>
    Connecting,

    /// <summary>
    /// The connection is open.
    /// </summary>
    Open,

    /// <summary>
    /// The connection to the LIS is broken. This can occur only after the connection has been opened. 
    /// </summary>
    Broken
}
