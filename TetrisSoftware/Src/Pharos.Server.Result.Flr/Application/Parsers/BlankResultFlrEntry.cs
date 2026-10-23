namespace Pharos.Server.Result.Flr.Application.Parsers;

internal enum FlrTechnology { Unspecified = 0, ImmunoCap = 1, EliA = 2, ViewAllergy = 3 }

internal class BlankResultFlrEntry : FlrEntry
{
    public FlrTechnology Technology { get; set; }

    public BlankResultFlrEntry(FlrMeasurementType measurementType) : base(measurementType)
    {
    }

    protected override void Parse(string[] columnValues, Dictionary<string, FlrEntryColumn> columns)
    {
        base.Parse(columnValues, columns);

        // we want to allow empty GroupId for blank results, due to some ambiguity in the FLR specification.
        GroupId = columnValues.GetString(columns, FlrEntryColumns.GroupId, canBeEmpty: true);
        Technology = (FlrTechnology)columnValues.GetInt(columns, FlrEntryColumns.Technology);
    }
}
