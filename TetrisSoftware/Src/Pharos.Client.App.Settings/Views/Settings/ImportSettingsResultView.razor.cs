using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Settings.Views.Settings;
public partial class ImportSettingsResultView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public bool IsErrorInvalidFile { get; set; }
    [Parameter] public bool IsUnrecognizedSettings { get; set; }
    [Parameter] public ICollection<FailedSettingImportError> InvalidSettings { get; set; } = [];
    [Parameter] public ICollection<UnrecognizedSettingImportInfo> UnrecognizedSettings { get; set; } = [];

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}
