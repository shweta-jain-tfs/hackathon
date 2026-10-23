using System.Text.Json.Serialization;
using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Application.Metadata;
public class PageList : SettingsChildItem
{
    public const string RequiredTablePropertyName = "Name";
    public const string ProviderManagedDisplayNameColumnHeader = "Name";

    public ChildPageItem? PageStructure { get; set; }

    public List<ChildPageItem> PageEntries { get; set; } = [];

    public List<string> ColumnHeaderLabels { get; set; } = [];

    public bool HasCopyFunction { get; set; }

    /// <summary>
    /// When true, entries are managed by a provider. Users cannot create, delete,
    /// or copy entries.
    /// </summary>
    public bool IsProviderManaged { get; set; }

    /// <summary>
    /// The keyed service registration key for the provider that supplies PageList entries.
    /// Used to automatically populate entries for provider-managed PageLists.
    /// </summary>
    [JsonIgnore]
    public string? EntryProviderKey { get; set; }

    /// <summary>
    /// Name of the entry property that stores the provider entry key.
    /// The option is always hidden in the settings UI.
    /// </summary>
    public string? ProviderKeyPropertyName { get; set; }

    /// <summary>
    /// An entry is the Header label/Key/Value for a row entry in that column.
    /// </summary>
    public List<Row> Columns { get; set; } = [];

    /// <summary>
    /// This is used in the frontend to assign DynamicDropdowns so they can be correctly removed from DB
    /// when PageEntry is deleted
    /// </summary>
    public List<Option> DynamicDropdowns { get; set; } = [];

    public override SettingsChildItem DeepCopy(string settingsKeyPrefix, string parentNavigationPath)
    {
        var key = settingsKeyPrefix + PageHelper.SettingsKeyDeLimiter + PageHelper.GetSuffixKeyName(Key);

        var dependency = PageHelper.CopyDependency(Dependency, settingsKeyPrefix);

        return new PageList
        {
            Key = key,
            Label = Label,
            LabelHelpText = LabelHelpText,
            Url = Url,
            Dependency = dependency,
            PageStructure = (ChildPageItem)PageStructure!.DeepCopy(settingsKeyPrefix, parentNavigationPath),
            PageEntries = PageEntries.Select(e => e.DeepCopy(key, parentNavigationPath)).Cast<ChildPageItem>().ToList(),
            ColumnHeaderLabels = ColumnHeaderLabels,
            Permissions = Permissions,
            HasCopyFunction = HasCopyFunction,
            IsProviderManaged = IsProviderManaged,
            ExcludeFromImportExport = ExcludeFromImportExport,
            EntryProviderKey = EntryProviderKey,
            ProviderKeyPropertyName = ProviderKeyPropertyName,
            MaxItems = MaxItems,
            MinItems = MinItems,
            ShowOnlyInRndMode = ShowOnlyInRndMode
        };
    }
    public int? MaxItems { get; set; }

    public int? MinItems { get; set; }
}

public class Row
{
    public string ColumnHeaderLabel { get; set; } = "";

    public Option? Value { get; set; }
}
