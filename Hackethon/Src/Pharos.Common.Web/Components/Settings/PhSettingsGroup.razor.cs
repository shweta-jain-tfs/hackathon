using Pharos.Common.Web.Components.Modal;

namespace Pharos.Common.Web.Components.Settings;
public partial class PhSettingsGroup<TItem>
{
    [Inject] public IModalService Modal { get; set; } = default!;
    [Parameter] public bool IsDependencyEnabled { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public string? Id { get; set; }
    [Parameter] public IEnumerable<TItem>? Items { get; set; }
    [Parameter] public RenderFragment<TItem>? CreateItem { get; set; }
    [Parameter] public EventCallback<List<TItem>> ResetToDefault { get; set; }


    public async Task ResetItemsToDefault()
    {
        if (ResetToDefault.HasDelegate)
        {
            await ResetToDefault.InvokeAsync(null);
        }
    }

    private async Task ResetViewOpen()
    {
        var modal = Modal.Show<SettingsGroupView>(new ModalOptions { Title = "Reset default", ModalHeight = ModalSize.Small });

        var result = await modal.Result;

        if (!result.Canceled)
        {
            await ResetItemsToDefault();
        }
    }
}
