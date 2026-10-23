using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.InstrumentManagement.Views.Dashboard;

public enum EndAssayOptions
{
    LoadedSamplesAreProcessed,
    StartedTestsAreProcessed
}

public partial class AssayEndModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    private EndAssayOptions SelectedValue { get; set; }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}
