using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Pharos.Server.Infrastructure.Services;

internal class SmbConnection : IDisposable
{
    readonly string _networkName;
    private readonly ILogger _logger;

    public SmbConnection(string networkName, NetworkCredential credentials, ILogger logger)
    {
        _networkName = networkName;
        _logger = logger;

        var netResource = new NetResource
        {
            Scope = 2, ResourceType = 1, DisplayType = 3, RemoteName = networkName
        };

        var hresult = WNetAddConnection2(netResource, credentials.Password, credentials.UserName, 0);

        if (hresult != 0)
        {
            _logger.LogError("Error connecting to remote share, HRESULT: {HResult}", hresult);
            throw new IOException("Error connecting to remote share", hresult);
        }
    }

    public void Dispose()
    {
        if (!string.IsNullOrEmpty(_networkName))
        {
            var hresult = WNetCancelConnection2(_networkName, 0, true);
            if (hresult != 0)
            {
                _logger.LogError("Error disconnecting from remote share, HRESULT: {HResult}", hresult);
            }
        }

        // Suppress finalization to prevent derived types from needing to re-implement IDisposable
        GC.SuppressFinalize(this);
    }
    /// <summary>
    /// Returns UNC path if possible, otherwise null
    /// Condition1: path cannot be null or empty/ws
    /// Option1: path is UNC and contains the correct host
    /// Option2: path is UNC and host parameter is empty
    /// Option3: path is relative and host can be added to create a UNC path
    /// </summary>
    /// <param name="host"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string? ToUncPath(string host, string path)
    {
        // path is mandatory
        if (!string.IsNullOrWhiteSpace(path))
        {
            // if path is UNC, and it contains the correct host, return path
            if (path.StartsWith(@"\\", StringComparison.Ordinal))
            {
                var parts = path.AsSpan(2).ToString().Split('\\');
                return parts.Length > 1 && string.Equals(parts[0], host, StringComparison.OrdinalIgnoreCase) ? path : null;
            }
            // if path is relative, add host to return a UNC path, or return null if no host
            return string.IsNullOrWhiteSpace(host) ? null : Path.Join(@"\\", host, path);
        }
        return null;
    }

    public static string GetShareRoot(string uncPath)
    {
        // Extracts \\server\share from \\server\share\folder\file.txt
        var parts = uncPath.TrimStart('\\').Split('\\');
        return $@"\\{parts[0]}\{parts[1]}";
    }

    [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int WNetAddConnection2(NetResource netResource, string password, string username, int flags);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int WNetCancelConnection2(string name, int flags, bool force);

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    [StructLayout(LayoutKind.Sequential)]
    private class NetResource
    {
        public int Scope;
        public int ResourceType;
        public int DisplayType;
        public int Usage;
        public string LocalName;
        public string RemoteName;
        public string Comment;
        public string Provider;
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}
