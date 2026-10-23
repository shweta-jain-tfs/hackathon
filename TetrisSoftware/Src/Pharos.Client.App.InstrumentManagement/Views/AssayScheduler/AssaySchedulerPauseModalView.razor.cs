using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.ViewModels.AssayScheduler;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.InstrumentManagement.Views.AssayScheduler;

public enum AssaySchedulerPauseMode
{
    AllInstruments,
    CurrentInstrument,
}

public partial class AssaySchedulerPauseModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] public ModalInstance ModalInstance { get; set; } = default!;

    [Parameter] public required string InstrumentName { get; set; }
    [Parameter] public required AssaySchedulerPauseMode Mode { get; set; }
    [Parameter] public required AssaySchedulerPauseViewModel ViewModel { get; set; }


    private async Task CloseModal(ModalResult result)
    {
        if (result.Canceled)
        {
            // Close without returning any changes.
            await ModalInstance.CloseAsync(ModalResult.Cancel());
        }
        else
        {
            // Return the updated view model.
            await ModalInstance.CloseAsync(ModalResult.Ok(ViewModel));
        }
    }
}
