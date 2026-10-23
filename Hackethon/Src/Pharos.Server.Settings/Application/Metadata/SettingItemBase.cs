using System.Text.Json.Serialization;

namespace Pharos.Server.Settings.Application.Metadata;

[JsonDerivedType(typeof(SettingsChildItem), nameof(SettingsChildItem))]
[JsonDerivedType(typeof(Group), nameof(Group))]
[JsonDerivedType(typeof(Option), nameof(Option))]
[JsonDerivedType(typeof(PageList), nameof(PageList))]
[JsonDerivedType(typeof(PagePlaceholder), nameof(PagePlaceholder))]
[JsonDerivedType(typeof(Page), nameof(Page))]
[JsonDerivedType(typeof(ActionButton), nameof(ActionButton))]
public class SettingItemBase
{
    public int Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
    public string LabelHelpText { get; set; } = string.Empty;

    /// <summary>
    /// The url associated with the item
    /// </summary>
    public string Url { get; set; } = "";

    public List<Dependency> Dependency { get; set; } = [];
    public IReadOnlyList<string> Permissions { get; set; } = [];

    [JsonIgnore]
    public bool ExcludeFromImportExport { get; set; }

    /// <summary>
    /// When true, this item is only visible when RnD mode is active.
    /// </summary>
    public bool ShowOnlyInRndMode { get; set; }
}