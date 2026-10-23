using Microsoft.Extensions.Configuration;

namespace Pharos.Server.Infrastructure.Services;

internal sealed class LogFilePathProvider(IConfiguration configuration) : ILogFilePathProvider
{
    public string GetLogFilePath()
    {
        var serilogSection = configuration.GetSection("Serilog");
        var fileSinkPath = GetFileSinkPath(serilogSection);
        if (string.IsNullOrWhiteSpace(fileSinkPath))
        {
            throw new InvalidOperationException("Serilog File sink configuration is missing the path.");
        }

        return Environment.ExpandEnvironmentVariables(fileSinkPath);
    }

    private static string? GetFileSinkPath(IConfiguration section)
    {
        if (string.Equals(section["Name"], "File", StringComparison.OrdinalIgnoreCase))
        {
            return section["Args:path"];
        }

        foreach (var child in section.GetChildren())
        {
            var path = GetFileSinkPath(child);
            if (path != null)
            {
                return path;
            }
        }

        return null;
    }
}