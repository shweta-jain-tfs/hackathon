using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;

public class TableRowViewModel(IStringLocalizer<Translations> loc) : SettingsViewModelBase(loc), IEquatable<TableRowViewModel>
{
    private readonly Guid _uniqueId = Guid.NewGuid();
    public bool IsSelected { get; set; }

    /// <summary>
    /// A row doesn't have a Model, but needs the key to be able to delete the entry
    /// </summary>
    public string? EntryKey { get; set; }

    public List<TableEntryViewModel> Values { get; set; } = [];

    public TableEntryViewModel GetValue(string key)
    {
        var match = Values.FirstOrDefault(v => v.Header == key);

        if (match == null)
        {
            throw new KeyNotFoundException($"Key '{key}' not found.");
        }

        return match;
    }

    public bool Equals(TableRowViewModel? other)
    {
        return EntryKey == other?.EntryKey && Values.SequenceEqual(other?.Values ?? []);
    }

    // Needed for IEquatable
    public override bool Equals(object? obj)
    {
        if (obj is TableRowViewModel other)
        {
            return Equals(other);
        }
        return false;
    }

    // Needed for IEquatable
    public override int GetHashCode()
    {
        var hash = 17;
        hash = (hash * 23) +_uniqueId.GetHashCode();
        foreach (var value in Values)
        {
            hash = (hash * 23) + (value?.Model?.GetHashCode() ?? 0);
        }
        return hash;
    }
}