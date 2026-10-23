using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.InstrumentManagement.Views.Dashboard;

public partial class ActivateMethodModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [CascadingParameter] public ModalInstance ModalInstance { get; set; } = default!;

    [Parameter] public string? MethodNameFilter { get; set; }

    private async Task CloseModal(ModalResult result)
    {
        if (result.Canceled)
        {
            await ModalInstance.CloseAsync(ModalResult.Cancel());
        }
        else
        {
            await ModalInstance.CloseAsync(ModalResult.Ok());
        }
    }
}
