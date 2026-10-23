using Pharos.Client.App.Parameters.Methods.ViewModels;

namespace Pharos.Client.App.Parameters.Methods.Views;

public partial class MethodTestsView
{
    [Parameter] public int MethodId { get; set; }
    [Inject] private MethodTestsGridViewModel TestsGridViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private static string FormatLabNames(ICollection<MethodTestLabName>? contextMethodTestLabNames)
    {
        return contextMethodTestLabNames == null || contextMethodTestLabNames.Count == 0 ? string.Empty
            : string.Join(", ", contextMethodTestLabNames.OrderBy(x => x.SortOrder).Select(labName => labName.TestName));
    }
    private static bool IsReportQuotient(MethodTest context)
    {
        return context.QuotientReportMode != QuotientReportMode.NoReport;
    }
}