using System.Management;
using Pharos.Server.Application;

namespace Pharos.Server.Services;

public sealed class OperatingSystemInformation : IOperatingSystemInformation
{
    public string Version => Environment.OSVersion.Version.ToString();

    public string FriendlyName => GetWindowsOperatingSystemCaption() ?? "Unknown";

    private static string? GetWindowsOperatingSystemCaption()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        using var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
        using var operatingSystems = searcher.Get();

        foreach (var o in operatingSystems)
        {
            var operatingSystem = (ManagementObject)o;
            using (operatingSystem)
            {
                return operatingSystem.GetPropertyValue("Caption")?.ToString();
            }
        }

        return null;
    }
}
