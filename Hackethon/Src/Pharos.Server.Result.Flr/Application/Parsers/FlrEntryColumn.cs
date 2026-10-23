namespace Pharos.Server.Result.Flr.Application.Parsers;

/// <summary>
/// Represents a column in the [ENTRIES] section of the FLR file.
/// </summary>
internal class FlrEntryColumn(string name, int index, bool essential = false)
{
    /// <summary>
    /// The column name, see <see cref="FlrEntryColumns"/>.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The index of the column value in the [ENTRIES] section, starting from 0.
    /// </summary>
    public int Index { get; } = index;

    /// <summary>
    /// Gets a value indicating whether the column must be present in the FLR-file (may still be blank).
    /// </summary>
    public bool Essential { get; } = essential;
}
