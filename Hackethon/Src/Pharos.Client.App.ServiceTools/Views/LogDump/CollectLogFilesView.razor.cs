using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.ViewModels;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.ServiceTools.Views.LogDump;

public partial class CollectLogFilesView
{
    [Inject] private CollectLogFilesViewModel ViewModel { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = null!;

    private Task CancelAsync()
    {
        return Modal.CloseAsync(ModalResult.Cancel());
    }

    private Task StartAsync()
    {
        return Modal.CloseAsync(ModalResult.Ok(ViewModel));
    }
}
