namespace Pharos.Server.Result.Flr.Application.Parsers;

internal class FlrFileImportFileContentParser
{
    private const string _headerSectionText = "[HEADER]";
    private const string _entriesSectionText = "[ENTRIES]";
    private const string _endSectionText = "[END]";
    private const string _sepKey = "SEP=;";
    private const char _separator = ';';

    private enum Section { None, Header, Entries, End }
    private Section _section = Section.None;

    private readonly FlrHeaderInformation _headerInformation = new();
    private readonly Dictionary<string, FlrEntryColumn> _entryColumns = new();
    private readonly List<FlrEntry> _entries = new();

    private int _lineNumber;
    private int _maxColumnIndex;
    private bool _separatorLineHandled;
    private bool _entriesHeaderParsed;

    public FlrFileImportFileContentParserResult Parse(StreamReader fileReader)
    {
        while (!fileReader.EndOfStream)
        {
            var line = fileReader.ReadLine();
            if (line == null)
            {
                continue;
            }

            _lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var l = line.Trim();
            if (!_separatorLineHandled)
            {
                if (!l.Equals(_sepKey, StringComparison.Ordinal))
                {
                    throw new FormatException($"The first non-empty line in the FLR file must be '{_sepKey}'.");
                }

                _separatorLineHandled = true;
            }
            else if (_section == Section.None)
            {
                if (!l.Equals(_headerSectionText, StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException($"{_headerSectionText} section expected at the line {_lineNumber}.");
                }

                _section = Section.Header;
            }
            else if (_section == Section.Header)
            {
                if (l.Equals(_entriesSectionText, StringComparison.OrdinalIgnoreCase))
                {
                    _section = Section.Entries;
                }
                else
                {
                    _headerInformation.ParseHeaderLine(l, _separator);
                }
            }
            else if (_section == Section.Entries)
            {
                if (l.Equals(_endSectionText, StringComparison.OrdinalIgnoreCase))
                {
                    _section = Section.End;
                }
                else
                {
                    if (!_entriesHeaderParsed)
                    {
                        ParseEntriesHeader(l);
                    }
                    else
                    {
                        ParseEntryValues(l);
                    }
                }
            }
            else
            {
                throw new FormatException($"Unexpected content after {_endSectionText} section: Line {_lineNumber} in the file.");
            }
        }

        if (_entries.Count == 0)
        {
            throw new FormatException("At least one FLR entry must be parsed from the file.");
        }

        return new FlrFileImportFileContentParserResult(_headerInformation, _entries);
    }

    private void ParseEntriesHeader(string l)
    {
        var columns = l.Split(_separator);

        var columnIndex = 0;
        foreach (var columnName in columns)
        {
            var columnInfo = FlrEntryColumns.All.FirstOrDefault(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
            if (columnInfo == default)
            {
                columnIndex++;
                continue;
            }

            var column = new FlrEntryColumn(columnInfo.ColumnName, columnIndex, columnInfo.Essential);
            columnIndex++;

            _entryColumns.Add(columnInfo.ColumnName, column);
        }

        if (_entryColumns.Count < FlrEntryColumns.All.Length)
        {
            var missingEssentialColumns = FlrEntryColumns.All
                .Where(c => c.Essential && _entryColumns.Keys.All(x => x != c.ColumnName))
                .Select(c => c.ColumnName)
                .ToList();

            if (missingEssentialColumns.Count > 0)
            {
                throw new FormatException($"{_entriesSectionText} section misses the following essential columns: {string.Join(", ", missingEssentialColumns)}");
            }
        }

        _entriesHeaderParsed = true;
        _maxColumnIndex = _entryColumns.Values.Max(c => c.Index);
    }

    private void ParseEntryValues(string l)
    {
        var entryValues = l.Split(_separator);
        if (entryValues.Length <= _maxColumnIndex)
        {
            throw new FormatException($"An FLR entry has fewer columns than expected: Line {_lineNumber} in the file.");
        }

        try
        {
            var entry = FlrEntry.TryParse(entryValues, _entryColumns);
            if (entry != null)
            {
                _entries.Add(entry);
            }
        }
        catch (Exception e)
        {
            throw new FormatException($"Failed to parse the FLR file line {_lineNumber}.", e);
        }
    }
}

