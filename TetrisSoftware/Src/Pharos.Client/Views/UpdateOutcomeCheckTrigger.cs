using Pharos.Client.App.SystemUpdate.Services;

namespace Pharos.Client.Views;

public sealed class UpdateOutcomeCheckTrigger : ComponentBase
{
    [Inject] public UpdateOutcomeChecker UpdateChecker { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await UpdateChecker.EnsureInitializedAsync();
    }
}