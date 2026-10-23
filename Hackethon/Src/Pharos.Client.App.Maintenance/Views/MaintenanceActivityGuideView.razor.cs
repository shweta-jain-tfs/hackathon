using Microsoft.AspNetCore.Components;
using Pharos.Client.App.Maintenance.Models;
using Pharos.Client.App.Maintenance.ViewModels;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Maintenance.Views;

public partial class MaintenanceActivityGuideView : IDisposable
{
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public MaintenanceActivityGuideViewModel ViewModel { get; set; } = null!;

    public void Dispose()
    {
        foreach (var viewModelStep in ViewModel.Steps)
        {
            viewModelStep.OnRefreshNeeded -= RefreshNeeded;
        }

        GC.SuppressFinalize(this);
    }

    protected override void OnParametersSet()
    {
        if (ViewModel == null)
        {
            throw new ArgumentNullException(nameof(ViewModel));
        }

        ListenToRefresh(ViewModel);
    }

    private void ListenToRefresh(MaintenanceActivityGuideViewModel viewModel)
    {
        foreach (var viewModelStep in viewModel.Steps)
        {
            viewModelStep.OnRefreshNeeded += RefreshNeeded;
        }
    }

    private void RefreshNeeded(object? sender, EventArgs e)
    {
        StateHasChanged();
    }

    private void SelectStep(ActivityStep step)
    {
        ViewModel.SelectStep(step);
    }

    private async Task HandleAction(StepAction action)
    {
        var success = await ViewModel.HandleAction(action);

        if (success && action.ActionType == StepActionType.Done)
        {
            await Modal.CloseAsync(ModalResult.Ok());
        }
    }
}