using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Contexts;

namespace Pharos.Common.Web.Components.NavigationLock;
public partial class PhNavigationConfirmation
{
    /// <summary>
    /// Gets or sets the modal instance.
    /// </summary>
    [CascadingParameter] private ModalInstance Modal { get; set; } = default!;
    [CascadingParameter(Name = "UiContext")] public UiContext? UiContext { get; set; }

    public NavigationLockStrings Strings { get; set; } = NavigationLockStrings.Default;

    protected override void OnParametersSet()
    {
        Strings = UiContext?.ComponentStrings.NavigationLock ?? NavigationLockStrings.Default;
    }


    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}