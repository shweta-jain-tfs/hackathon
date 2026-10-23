using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

internal sealed class ImportExportExclusions(IReadOnlySet<string> keyPrefixes, IReadOnlySet<string> exactKeys)
{
    public IReadOnlySet<string> KeyPrefixes { get; } = keyPrefixes;

    public IReadOnlySet<string> ExactKeys { get; } = exactKeys;

    public bool IsExcluded(string key)
    {
        var normalizedKey = RemoveNumericSegments(key);

        return ExactKeys.Contains(key)
            || ExactKeys.Contains(normalizedKey)
            || KeyPrefixes.Any(prefix =>
                key.StartsWith(prefix, StringComparison.Ordinal)
                || normalizedKey.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static string RemoveNumericSegments(string key)
    {
        return string.Join(
            PageHelper.SettingsKeyDeLimiter,
            key.Split(PageHelper.SettingsKeyDeLimiter)
                .Where(segment => !int.TryParse(segment, out _)));
    }
}
