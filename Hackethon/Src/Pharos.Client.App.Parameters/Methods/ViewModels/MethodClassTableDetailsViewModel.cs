namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public sealed class MethodClassTableDetailsViewModel : ViewModelBase
{
    public MethodClassTableDetailsViewModel(MethodClassTable classTable)
    {
        ClassTableId = classTable.ClassTableId;

        var orderedRows = (classTable.ClassTableRow ?? [])
            .OrderBy(x => x.ClassTableRowId)
            .ToList();

        if (orderedRows.Count > 0)
        {
            Rows.Add(new MethodClassTableDetailsRowViewModel(
                Rows.Count,
                null,
                $"0 - {orderedRows[0].Concentration}",
                classTable.TextBelow));
        }

        for (var i = 0; i < orderedRows.Count; i++)
        {
            var row = orderedRows[i];
            var nextRow = orderedRows.ElementAtOrDefault(i + 1);
            var rangeText = nextRow is null
                ? $"{row.Concentration}+"
                : $"{row.Concentration} - {nextRow.Concentration}";

            Rows.Add(new MethodClassTableDetailsRowViewModel(
                Rows.Count,
                row.ClassTableRowId,
                rangeText,
                row.TextAbove));
        }
    }

    public int ClassTableId { get; }

    public List<MethodClassTableDetailsRowViewModel> Rows { get; } = [];
}

public sealed class MethodClassTableDetailsRowViewModel : ViewModelBase
{
    public MethodClassTableDetailsRowViewModel(int rowIndex, int? classTableRowId, string concentrationRange, string? text)
    {
        RowIndex = rowIndex;
        ClassTableRowId = classTableRowId;
        ConcentrationRange = concentrationRange;
        Text = text;
    }

    public int RowIndex { get; }

    public int? ClassTableRowId { get; }

    public string ConcentrationRange { get; }

    public string? Text { get; set; }
}
