using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Application.Commands;

public class SettingsImportResult
{
    public bool Success { get; set; }

    public List<FailedSettingImportError> Errors { get; set; } = [];
    public List<UnrecognizedSettingImportInfo> UnrecognizedSettings { get; set; } = [];
}

public record FailedSettingImportError(string Key, string Value, SettingImportError Error);
public record UnrecognizedSettingImportInfo(string Key, string Value);

public class SettingsUpdateResult
{
    public bool Success { get; set; }

    public List<FailedSettingUpdateError> Errors { get; set; } = [];
}

public record FailedSettingUpdateError(string Key, string Value, SettingUpdateError Error);

public enum SettingUpdateError
{
    ValidationError,
    MaxItemsExceeded,
    ConcurrentModification,
    AbsoluteDirectoryWriteAccessDenied,
    SectionsOverlap
}
