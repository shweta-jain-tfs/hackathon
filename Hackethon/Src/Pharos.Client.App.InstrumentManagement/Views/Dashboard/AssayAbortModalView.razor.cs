using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.InstrumentManagement.Views.Dashboard;

public partial class AssayAbortModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public required InstrumentMethod InstrumentMethod { get; set; }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}
