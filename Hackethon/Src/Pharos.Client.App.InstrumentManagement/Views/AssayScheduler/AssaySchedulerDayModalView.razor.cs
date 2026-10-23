using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.ViewModels.AssayScheduler;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Modal;
using DayOfWeek = Pharos.Client.Common.Models.DayOfWeek;

namespace Pharos.Client.App.InstrumentManagement.Views.AssayScheduler;

public partial class AssaySchedulerDayModalView : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;


    [Parameter] public required AssaySchedulerDayViewModel ViewModel { get; set; }
    [Parameter] public required string TableHeader { get; set; }

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
            await ModalInstance.CloseAsync(ModalResult.Ok(ViewModel));
        }
    }

    private async Task ClearModal()
    {
        var options = new ModalOptions
        {
            Title = Loc["AssaySchedulerPage_AssaySchedulerClearModal_Title"],
            ModalHeight = ModalSize.XSmall,
            ModalWidth = ModalSize.Small
        };

        var translatedDay = GetTranslatedDay(ViewModel.Day);
        var modal = Modal.ShowConfirm(
            string.Format(Loc["AssaySchedulerPage_AssaySchedulerClearModal_Text"], ViewModel.InstrumentName, translatedDay),
            Loc["AssaySchedulerPage_AssaySchedulerClearModal_Cancel"],
            Loc["AssaySchedulerPage_AssaySchedulerClearModal_Ok"],
            options
            );

        var result = await modal.Result;
        if (!result.Canceled)
        {
            // Reset methods to their initial state.
            foreach (var method in ViewModel.ScheduledMethods!)
            {
                method.IsActive = false;
            }

            // Reset StartTime and EndTime.
            ViewModel.StartTime = null;
            ViewModel.EndTime = null;

            StateHasChanged();
        }
    }

    /// <summary>
    /// Get the translated day in plural.
    /// </summary>
    /// <param name="viewModelDay"></param>
    /// <returns></returns>
    private string GetTranslatedDay(DayOfWeek viewModelDay)
    {
        var dayName = viewModelDay.ToString();

        // Build the localization key.
        var key = $"AssaySchedulerPage_Day_{dayName}_Plural";

        // Fallback to the enum name if not found
        var translation = Loc[key];
        return string.IsNullOrEmpty(translation) ? dayName : translation;
    }

    public async Task ChangeRunOptions(string instrumentName, DayOfWeek dayOfWeek, AssaySchedulerMethodViewModel assaySchedulerMethod)
    {
        var copiedViewModel = new AssaySchedulerMethodViewModel(new AssaySchedulerMethod
        {
            IsActive = assaySchedulerMethod.IsActive,
            MethodName = assaySchedulerMethod.MethodName,
            WhenToRun = (int)assaySchedulerMethod.WhenToRun
        });

        var modal = Modal.Show<AssaySchedulerDaySelectRunModalView>(
            new ModalOptions
            {
                Title = Loc["AssaySchedulerPage_AssaySchedulerDaySelectRunModal_Title"],
                ModalHeight = ModalSize.Medium,
                ModalWidth = ModalSize.Medium
            },
            new ModalParameters
            {
                { nameof(AssaySchedulerDaySelectRunModalView.AssaySchedulerMethod), copiedViewModel },
                { nameof(AssaySchedulerDaySelectRunModalView.Day), dayOfWeek },
                { nameof(AssaySchedulerDaySelectRunModalView.InstrumentName), instrumentName },
            });

        var result = await modal.Result;
        if (result is { Canceled: false, Data: AssaySchedulerMethodViewModel updatedMethod })
        {
            // Apply changes to the original method.
            assaySchedulerMethod.MethodName = updatedMethod.MethodName;
            assaySchedulerMethod.WhenToRun = updatedMethod.WhenToRun;
            assaySchedulerMethod.IsActive = updatedMethod.IsActive;
        }

        copiedViewModel.Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}