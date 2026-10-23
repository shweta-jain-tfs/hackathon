using Pharos.Common.Web.Components.Modal;
using Pharos.Client.App.Request.Models;

namespace Pharos.Client.App.Request.Views.RequestList;

public partial class RequestListResetView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IRequestDetailsDataService RequestDetailsDataService { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public int[] RequestIds { get; set; } = default!;

    private int SamplesCount => RequestIds.Length;

    private int TestsCount { get; set; }

    private bool IncludeAbortedTests { get; set; }

    private bool IsLoading { get; set; } = true;

    // Split the localized text for formatted display
    private string[] StatusInfoParts => Loc["RequestListPage_ResetDialog_StatusesInfo"].ToString().Split("{0}", 2);
    private string StatusInfoPrefix => StatusInfoParts.Length > 0 ? StatusInfoParts[0] : string.Empty;
    private string StatusInfoSuffix => StatusInfoParts.Length > 1 ? StatusInfoParts[1] : string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadTestsCount();
    }

    private async Task LoadTestsCount()
    {
        IsLoading = true;
        StateHasChanged(); // Force re-render to show spinner
        try
        {
            TestsCount = await RequestDetailsDataService.GetResettableTestsCountAsync(RequestIds, IncludeAbortedTests);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged(); // Force re-render to hide spinner
        }
    }

    private async Task OnIncludeAbortedTestsChanged(bool value)
    {
        IncludeAbortedTests = value;
        await LoadTestsCount();
    }

    private async Task Close(ModalResult modalResult)
    {
        await Modal.CloseAsync(modalResult);
    }
}

public class ResetOptions
{
    public bool IncludeAbortedTests { get; set; }
}
