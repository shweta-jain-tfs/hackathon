using Microsoft.Extensions.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Result.Views.Result;
public partial class RejectResultsModalView
{
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public int TestsSelectedCount { get; set; }
    [Parameter] public int TestsToRejectCount { get; set; }
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private bool AddNewTests { get; set; }
    public int NotRejectableCount => TestsSelectedCount - TestsToRejectCount;
    public bool ShowNotRejectableText => NotRejectableCount > 0;
    public bool CanReject => TestsToRejectCount > 0;

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}
