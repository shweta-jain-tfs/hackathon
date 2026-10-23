using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Request.Views.RequestDetail;

public partial class RequestDetailResetView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public int TestsResetCount { get; set; } = default!;

    [Parameter] public int TestsSelectedCount { get; set; } = default!;

    // Split the localized text for formatted display
    private string[] StatusInfoParts => Loc["RequestDetailPage_ResetDialog_StatusesInfo"].ToString().Split("{0}", 2);
    private string StatusInfoPrefix => StatusInfoParts.Length > 0 ? StatusInfoParts[0] : string.Empty;
    private string StatusInfoSuffix => StatusInfoParts.Length > 1 ? StatusInfoParts[1] : string.Empty;

    private async Task Close(ModalResult modalResult)
    {
        await Modal.CloseAsync(modalResult);
    }
}

public class ResetOptions
{
    public bool IncludeAbortedTests { get; set; }
}
