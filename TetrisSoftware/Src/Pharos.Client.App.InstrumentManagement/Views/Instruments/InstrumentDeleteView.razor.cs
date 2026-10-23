using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.InstrumentManagement.Views.Instruments;
public partial class InstrumentDeleteView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public string DisplayName { get; set; } = default!;
    [Parameter] public string SerialNumber { get; set; } = default!;
    [Parameter] public string ResultsExistsText { get; set; } = default!;

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}