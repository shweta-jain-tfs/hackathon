using Pharos.Server.Settings.Application.Commands.CopySettings;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Settings.Application.Interfaces;

internal interface ISettingsCopy
{
    SettingsCopyResult CreateCopy(
        Dictionary<string, Page> populatedPages,
        string sourcePageKey,
        Func<string, bool> settingKeyExistsPredicate,
        int actualItemCount);

    string? FindPageListKeyForSourcePage(Dictionary<string, Page> rootPages, string entryKey);
}

internal sealed record SettingsCopyResult(
    bool Success,
    CopySettingsErrorCode? ErrorCode,
    Page? NewPage,
    IReadOnlyList<Setting> SettingsToPersist,
    bool IsWarning = false);
