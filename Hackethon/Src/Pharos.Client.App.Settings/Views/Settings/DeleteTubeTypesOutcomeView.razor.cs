using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Settings.Views.Settings;

public partial class DeleteTubeTypesOutcomeView : ComponentBase
{
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public IReadOnlyList<string> DeletedEntries { get; set; } = [];

    [Parameter] public IReadOnlyList<TubeTypeDeleteBlockedEntry> BlockedEntries { get; set; } = [];

    public record TubeTypeDeleteBlockedEntry(string DisplayName, string ReasonResourceKey);

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}
