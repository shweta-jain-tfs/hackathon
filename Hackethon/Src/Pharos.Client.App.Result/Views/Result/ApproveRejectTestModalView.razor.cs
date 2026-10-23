using Microsoft.Extensions.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Result.Views.Result;

public partial class ApproveRejectTestModalView
{
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    /// <summary>
    /// The sample id (as displayed in the results list) the approve/reject action applies to.
    /// </summary>
    [Parameter] public string SampleIdDisplay { get; set; } = string.Empty;

    /// <summary>
    /// The name of the test the approve/reject action applies to.
    /// </summary>
    [Parameter] public string TestName { get; set; } = string.Empty;

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}

public enum ApproveRejectTestAction
{
    Approve,
    Reject
}
