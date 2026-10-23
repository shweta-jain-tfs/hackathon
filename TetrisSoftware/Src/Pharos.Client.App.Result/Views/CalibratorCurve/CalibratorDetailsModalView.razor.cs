using Microsoft.Extensions.Localization;
using Pharos.Client.App.Result.Models;
using Pharos.Client.App.Result.Views.Result;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Result.Views.CalibratorCurve;

public partial class CalibratorDetailsModalView
{
    [Inject] public IModalService ModalService { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private ITestResultReplicateDataService TestResultReplicateDataService { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public CalibratorTestResult CalibratorTestResult { get; set; } = default!;
    [Parameter] public bool CanEdit { get; set; }

    private async Task ReplicateDetailsView(int contextId)
    {
        var ids = CalibratorTestResult.Replicates.Select(x => x.Id).ToArray();
        var repDetails = await TestResultReplicateDataService.GetTestReplicateDetails(ids);

        var dropdownList = CalibratorTestResult.Replicates.Select(replicate => new DropDownOption<int>
        {
            Text = replicate.ReplicateNumber.ToString(),
            Value = replicate.Id
        }).ToList();

        var parameters = new ModalParameters
        {
            { nameof(TestReplicateModalView.Replicates), repDetails },
            { nameof(TestReplicateModalView.DropDownList), dropdownList },
            { nameof(TestReplicateModalView.PreSelectedReplicate), contextId }
        };

        var modal = ModalService.Show<TestReplicateModalView>(new ModalOptions
        {
            Title = Loc["TestReplicateModalView_Title"],
            ModalHeight = ModalSize.XLarge,
            ModalWidth = ModalSize.XLarge
        }, parameters);

        await modal.Result;
    }

    private async Task OnClose(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private static string GetStatusText(CalibratorTestReplicate context)
    {
        if (context.IsAborted)
        {
            return TestReplicateStatus.Aborted.ToString();
        }

        return context.HasError ? TestReplicateStatus.Error.ToString() : TestReplicateStatus.Ready.ToString();
    }

    private string GetConcentrationText(CalibratorTestReplicate context)
    {
        var conc = context.ConcentrationAsText.FormatDecimalForDisplay();

        if (string.IsNullOrWhiteSpace(conc))
        {
            return string.Empty;
        }

        return $"{conc} {CalibratorTestResult.Unit}";
    }
}