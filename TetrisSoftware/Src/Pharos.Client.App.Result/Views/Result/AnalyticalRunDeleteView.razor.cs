using System.Globalization;
using Microsoft.Extensions.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Result.Views.Result;

public partial class AnalyticalRunDeleteView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] private ModalInstance Modal { get; set; } = default!;
    [Parameter] public int NumberOfRunsToBeDeleted { get; set; } = default!;
    [Parameter] public int NumberToShow { get; set; } = default!;

    private string ConfirmDeleteText =>
        Loc["AnalyticalRunDeleteView_ConfirmDeleteRuns"].Value
            .Replace("{0}", NumberOfRunsToBeDeleted.ToString(CultureInfo.CurrentCulture));

    private string NotReportedOrApprovedText =>
        Loc["AnalyticalRunDeleteView_NotReportedOrApprovedWarning"].Value
            .Replace("{0}", NumberToShow.ToString(CultureInfo.CurrentCulture));

    private async Task Close(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}