using Microsoft.Extensions.Localization;
using Pharos.Client.App.Result.Models;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Result.Views.Result;

public partial class SampleTestResultDetailsModalView
{
    [Inject] public IModalService ModalService { get; set; } = default!;
    [Inject] private ITestResultReplicateDataService TestResultReplicateDataService { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public SampleTestResult TestResult { get; set; } = default!;
    [Parameter] public bool CanEdit { get; set; }
    public bool CannotBeModified => TestResult.ApprovalStatus != ResultApprovalStatus.NotApproved;

    private SampleTestResult _unModifiedTest = default!;

    private bool Disabled =>
        (_unModifiedTest.Comment == TestResult.Comment || (_unModifiedTest.Comment == null && string.IsNullOrEmpty(TestResult.Comment)))
        && _unModifiedTest.Replicates.Zip(TestResult.Replicates, (orig, curr) => orig.Active == curr.Active).All(equal => equal);

    protected override void OnInitialized()
    {
        _unModifiedTest = new SampleTestResult
        {
            Comment = TestResult.Comment,
            Replicates = TestResult.Replicates.Select(x => new SampleTestReplicate
            {
                Active = x.Active
            }).ToList()
        };
    }

    private async Task OnClose(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private string GetConcentrationText(SampleTestReplicate context)
    {
        var conc = context.ConcentrationAsText.FormatDecimalForDisplay();

        if (string.IsNullOrWhiteSpace(conc))
        {
            return string.Empty;
        }

        return $"{conc} {TestResult.Unit}";
    }

    private static string GetStatusText(SampleTestReplicate context)
    {
        if (context.IsAborted)
        {
            return TestReplicateStatus.Aborted.ToString();
        }

        return context.HasError ? TestReplicateStatus.Error.ToString() : TestReplicateStatus.Ready.ToString();
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
}