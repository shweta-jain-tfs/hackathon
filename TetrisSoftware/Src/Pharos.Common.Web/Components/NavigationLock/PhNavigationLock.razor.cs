using Microsoft.AspNetCore.Components.Routing;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Contexts;

namespace Pharos.Common.Web.Components.NavigationLock;

public partial class PhNavigationLock
{
    [Inject] public IModalService Modal { get; set; } = default!;

    [CascadingParameter(Name = "UiContext")] public UiContext? UiContext { get; set; }
    [Parameter] public bool Modified { get; set; }
    [Parameter] public Func<Task<bool>>? OnSave { get; set; }
    [Parameter] public EventCallback ReloadPageRouteMap { get; set; }
    [Parameter] public EventCallback InvalidEntryPrompt { get; set; }
    [Parameter] public EventCallback DiscardChanges { get; set; }
    [Parameter] public bool IsValid { get; set; } = true;

    public NavigationLockStrings Strings { get; set; } = NavigationLockStrings.Default;

    protected override void OnParametersSet()
    {
        Strings = UiContext?.ComponentStrings.NavigationLock ?? NavigationLockStrings.Default;
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext locationChangingContext)
    {
        if (!Modified)
        {
            return;
        }

        var modal = Modal.Show<PhNavigationConfirmation>(new ModalOptions() { Title = Strings.UnsavedChangesTitle, ModalHeight = ModalSize.Small, ModalWidth = ModalSize.Small });
        var result = await modal.Result;

        if (!result.Canceled)
        {
            if (result.Data == null)
            {
                locationChangingContext.PreventNavigation();
            }
            else
            {
                var data = (bool)result.Data;
                if (data)
                {
                    if (IsValid)
                    {
                        var success = OnSave is not null && await OnSave.Invoke();
                        if (success)
                        {
                            // Navigation can proceed - save was successful
                            return;
                        }
                        else
                        {
                            // Save failed, prevent navigation
                            locationChangingContext.PreventNavigation();
                        }
                    }
                    else
                    {
                        await InvalidEntryPrompt.InvokeAsync();
                        locationChangingContext.PreventNavigation();
                    }
                }
                else
                {
                    await DiscardChanges.InvokeAsync();
                }
            }
        }
        else
        {
            locationChangingContext.PreventNavigation();
        }

        await ReloadPageRouteMap.InvokeAsync();
    }
}