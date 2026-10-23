using Microsoft.Extensions.Localization;
using Pharos.Client.App.Result.Models;
using Pharos.Client.App.Result.Views.Result;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Result.Views.CurveControl;

public partial class CurveControlDetailsModalView
{
    [Inject] public IModalService ModalService { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private ITestResultReplicateDataService TestResultReplicateDataService { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public CurveControlTestResult TestResult { get; set; } = default!;
    [Parameter] public bool CanEdit { get; set; }

    private string? _originalComment;

    private bool Disabled => _originalComment == TestResult.Comment || (_originalComment == null && string.IsNullOrEmpty(TestResult.Comment));

    protected override void OnInitialized()
    {
        _originalComment = TestResult.Comment;
    }

    private async Task ReplicateDetailsView(int contextId)
    {
        var ids = TestResult.Replicates.Select(x => x.Id).ToArray();
        var repDetails = await TestResultReplicateDataService.GetTestReplicateDetails(ids);

        var dropdownList = TestResult.Replicates.Select(replicate => new DropDownOption<int>
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

    private static string GetStatusText(CurveControlTestReplicate context)
    {
        if (context.IsAborted)
        {
            return TestReplicateStatus.Aborted.ToString();
        }

        return context.HasError ? TestReplicateStatus.Error.ToString() : TestReplicateStatus.Ready.ToString();
    }

    private string GetConcentrationText(CurveControlTestReplicate context)
    {
        var conc = context.ConcentrationAsText.FormatDecimalForDisplay();

        if (string.IsNullOrWhiteSpace(conc))
        {
            return string.Empty;
        }

        return $"{conc} {TestResult.Unit}";
    }
}