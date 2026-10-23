using Microsoft.Extensions.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Result.Views.Result;
public partial class ApproveResultsModalView
{
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public int TestsSelectedCount { get; set; }
    [Parameter] public int TestToApproveCount { get; set; }
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    public bool CanApprove => TestToApproveCount > 0;

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}
