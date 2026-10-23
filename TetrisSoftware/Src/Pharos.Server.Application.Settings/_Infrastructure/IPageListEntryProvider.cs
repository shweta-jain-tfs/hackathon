namespace Pharos.Server.Application.Settings;

/// <summary>
/// Provides a list of entries for a provider-managed PageList.
/// Implementations supply the set of entries (e.g., stable external identifiers)
/// that should exist in the PageList. The settings framework ensures that
/// an entry exists for each key returned by this provider.
/// </summary>
public interface IPageListEntryProvider
{
    /// <summary>
    /// Returns the entries that should exist in the PageList.
    /// </summary>
    Task<IReadOnlyList<PageListEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an entry to be created in a provider-managed PageList.
/// </summary>
public record PageListEntry
{
    /// <summary>
    /// A stable identifier for the entry (e.g., an instrument serial number).
    /// Used to match entries in the settings table against their source.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// The display name / label for the entry (e.g., "Billy").
    /// Provider-managed PageLists use this as the entry label in the UI.
    /// </summary>
    public required string DisplayName { get; init; }
}
