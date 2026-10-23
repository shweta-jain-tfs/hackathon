using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Infrastructure.Serialization;
using System.Text.Json.Serialization;

namespace Pharos.Server.Settings.Application.Metadata;

public enum OptionType
{
    Boolean,
    Integer,
    Double,
    String,
    Date,
    Choice,
    Time
}

public class Option : SettingsChildItem
{
    public bool Required { get; set; }

    public bool Unique { get; set; }

    public bool UniqueIsCaseInsensitive { get; set; }

    public string? Value { get; set; } = "";

    public OptionType Type { get; set; }

    public string MaxValue { get; set; } = "0";

    public string MinValue { get; set; } = "0";

    public List<Choice> Choices { get; set; } = [];

    public string? DefaultValue { get; set; } = "";

    public string UnitResourceName { get; set; } = string.Empty;

    public int MaxLength { get; set; } = int.MaxValue;

    /// <summary>
    /// Regular expression used to identify disallowed characters in the value.
    /// </summary>
    public string? DisallowedCharactersPattern { get; set; }

    public object ValueAfterCopy { get; set; } = null!;

    /// <summary>
    /// If true this option is a column in a PageList.
    /// </summary>
    public bool IsColumnHeader { get; set; }

    public bool IsNameColumn { get; set; }

    public string DynamicDropdownOrigin { get; set; } = string.Empty;

    public string DynamicDropdownValueTarget { get; set; } = string.Empty;

    public bool IsObfuscated { get; set; }

    /// <summary>
    /// Indicates whether the value should be encrypted at rest in the database.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// The type of the provider class that implements IDropdownValueProvider.
    /// Used for provider-based dynamic dropdowns.
    /// </summary>
    [JsonConverter(typeof(TypeToStringConverter))]
    public Type? DynamicDropdownProviderType { get; set; }

    /// <summary>
    /// Determines whether the option is displayed as a group of radio buttons instead of a dropdown in the Settings.
    /// </summary>
    public bool IsDisplayModeRadioButtons { get; set; }

    /// <summary>
    /// When true, the option cannot be edited by the user in the settings UI.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// When true, the option is part of the settings model but is not rendered in the settings UI.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// When true, the value must be a fully qualified absolute directory path
    /// </summary>
    public bool IsAbsoluteDirectoryPath { get; set; }

    public List<NonOverlappingRangesValidation> NonOverlappingRanges { get; set; } = [];

    public override SettingsChildItem DeepCopy(string settingsKeyPrefix, string parentNavigationPath)
    {
        var dependency = string.IsNullOrEmpty(settingsKeyPrefix) ? Dependency : PageHelper.CopyDependency(Dependency, settingsKeyPrefix);

        return new Option
        {
            Key = settingsKeyPrefix + PageHelper.SettingsKeyDeLimiter + PageHelper.GetSuffixKeyName(Key),
            Required = Required,
            Unique = Unique,
            UniqueIsCaseInsensitive = UniqueIsCaseInsensitive,
            Value = Value,
            Type = Type,
            IsColumnHeader = IsColumnHeader,
            IsNameColumn = IsNameColumn,
            MaxValue = MaxValue,
            MinValue = MinValue,
            Choices = [..Choices],
            Label = Label,
            LabelHelpText = LabelHelpText,
            Dependency = dependency,
            DefaultValue = DefaultValue,
            UnitResourceName = UnitResourceName,
            MaxLength = MaxLength,
            DisallowedCharactersPattern = DisallowedCharactersPattern,
            DynamicDropdownOrigin = DynamicDropdownOrigin,
            DynamicDropdownValueTarget = DynamicDropdownValueTarget,
            DynamicDropdownProviderType = DynamicDropdownProviderType,
            IsDisplayModeRadioButtons = IsDisplayModeRadioButtons,
            IsReadOnly = IsReadOnly,
            IsHidden = IsHidden,
            IsObfuscated = IsObfuscated,
            IsEncrypted = IsEncrypted,
            IsAbsoluteDirectoryPath = IsAbsoluteDirectoryPath,
            Permissions = Permissions,
            ExcludeFromImportExport = ExcludeFromImportExport,
            ValueAfterCopy = ValueAfterCopy,
            ShowOnlyInRndMode = ShowOnlyInRndMode,
            NonOverlappingRanges = NonOverlappingRanges.Select(validation => validation.DeepCopy(settingsKeyPrefix)).ToList()
        };
    }
}
