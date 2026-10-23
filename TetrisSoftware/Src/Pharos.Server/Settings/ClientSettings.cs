using Pharos.Contracts;

namespace Pharos.Server.Settings;

internal class ClientSettings
{
    public const string ClientSection = "Client";

    /// <summary>
    /// Override ForceClientDeviceType property appsettings.Development.json  (you may need to create the file first)
    /// to force the client UI to switch to a specific mode. By default the client device is detected automatically.
    /// The setting works in the Development environment only.
    /// </summary>
    public ClientDeviceType? ForcedClientDeviceType { get; set; }
}
