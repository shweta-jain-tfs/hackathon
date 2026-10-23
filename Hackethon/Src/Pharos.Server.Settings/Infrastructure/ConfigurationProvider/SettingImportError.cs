namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

/// <summary>
/// Type of error that can occur during the import of settings.
/// </summary>
public enum SettingImportError
{
    None,
    InvalidType,
    InvalidValue,
    RequiredSettingNotDefined,
    RequiredSettingMissing,
    DuplicateValue,
    DuplicateKey,
    InvalidEnumValue,
    InvalidFileType,
    MalformedFile,
    UnrecognizedSetting, // Setting does not exist or match any known setting, ignored during import
    MaxItemsExceeded,
    ConcurrentModification,
    MissingKeyValueDelimiter,
    SectionsOverlap,
}