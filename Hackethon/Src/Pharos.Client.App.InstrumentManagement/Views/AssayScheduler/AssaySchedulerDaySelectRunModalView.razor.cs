using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.ViewModels.AssayScheduler;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;
using DayOfWeek = Pharos.Client.Common.Models.DayOfWeek;

namespace Pharos.Client.App.InstrumentManagement.Views.AssayScheduler;

public partial class AssaySchedulerDaySelectRunModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Parameter] public required AssaySchedulerMethodViewModel AssaySchedulerMethod { get; set; }
    [Parameter] public required DayOfWeek Day { get; set; }
    [Parameter] public required string InstrumentName { get; set; }

    [CascadingParameter] public ModalInstance ModalInstance { get; set; } = default!;



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
            await ModalInstance.CloseAsync(ModalResult.Ok(AssaySchedulerMethod));
        }
    }
}
