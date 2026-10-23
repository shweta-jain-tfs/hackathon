using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

/// <summary>
/// The modes in Pharos can establish a connection with LIS.
/// </summary>
/// <remarks>
/// Do not modify numbers for the existing enum entries (they are persisted).
/// </remarks>
public enum LisConnectionMode
{
    /// <summary>
    /// Pharos acts as a server.
    /// </summary>
    [DisplayNameLocal("LisConnectionMode_Server")]
    Server = 1,

    /// <summary>
    /// Pharos acts as a client.
    /// </summary>
    [DisplayNameLocal("LisConnectionMode_Client")]
    Client = 2
}