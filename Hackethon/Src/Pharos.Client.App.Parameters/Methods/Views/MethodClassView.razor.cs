using Pharos.Client.App.Parameters.Methods.ViewModels;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Methods.Views;

public partial class MethodClassView
{
    private const int _numberOfClassRangeColumns = 9;

    [Parameter] public MethodDetailsViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;

    private IReadOnlyCollection<MethodClassTable> ClassTables { get; set; } = [];
    private IQueryable<MethodClassTable> ClassTableItems => ClassTables.AsQueryable();

    protected override void OnParametersSet()
    {
        ClassTables = (ViewModel.Model.ClassTables ?? [])
            .OrderBy(classTable => classTable.ClassTableId)
            .ToList();
    }

    private string GetClassColumnTitle(int classNumber)
    {
        return string.Format(Loc["MethodDetailsClassTab_ClassColumn"], classNumber);
    }

    private async Task OpenClassTableDetails(MethodClassTable classTable)
    {
        var viewModel = new MethodClassTableDetailsViewModel(classTable);
        var modal = Modal.Show<MethodClassTableDetailsModalView>(
            new ModalOptions
            {
                Title = Loc["MethodDetailsClassTableDetails_Title"],
                ModalWidth = ModalSize.Small,
                ModalHeight = ModalSize.XLarge
            },
            new ModalParameters
            {
                { nameof(MethodClassTableDetailsModalView.ViewModel), viewModel }
            });

        var result = await modal.Result;
        if (result is { Canceled: false, Data: MethodClassTableDetailsViewModel classTableDetails })
        {
            ViewModel.ApplyClassTableDetails(classTableDetails);
        }
    }

    private static string GetClassOneRangeText(MethodClassTable context)
    {
        var rows = GetOrderedRows(context);
        var firstConcentration = rows.Count > 0 ? rows[0].Concentration : (double?)null;
        return firstConcentration == null ? string.Empty : $"(0 - {firstConcentration})";
    }

    private static string GetClassRangeText(MethodClassTable context, int rowIndex)
    {
        var rows = GetOrderedRows(context);
        var startConc = rows.ElementAtOrDefault(rowIndex)?.Concentration;
        var endConc = rows.ElementAtOrDefault(rowIndex + 1)?.Concentration;

        if (startConc == null)
        {
            return string.Empty;
        }

        return endConc == null ? $"({startConc}+)" : $"({startConc} - {endConc})";
    }

    private static List<ClassTableRowInfo> GetOrderedRows(MethodClassTable context)
    {
        return (context.ClassTableRow ?? [])
            .OrderBy(classTableRow => classTableRow.ClassTableRowId)
            .ToList();
    }
}
