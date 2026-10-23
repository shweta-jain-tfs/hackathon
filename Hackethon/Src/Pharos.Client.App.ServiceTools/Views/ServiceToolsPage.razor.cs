using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.Views.RequestGenerator;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.ServiceTools.Views;

public partial class ServiceToolsPage
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private IModalService Modal { get; set; } = null!;

    private void OpenRequestGenerator()
    {
        Modal.Show<GeneratorView>(new ModalOptions
        {
            Title = Loc["RequestGenerator_Title"],
            ModalHeight = ModalSize.XLarge,
            ModalWidth = ModalSize.XLarge
        });
    }
}
