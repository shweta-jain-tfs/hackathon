using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Settings.Views.Settings;

public partial class ExportSettingsResultView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public ICollection<string> ErrorMessages { get; set; } = [];

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}
