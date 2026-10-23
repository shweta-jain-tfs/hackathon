using System.ComponentModel;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.ViewModels;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.UploadFile;

namespace Pharos.Client.App.ServiceTools.Views.LoadCmr;

public partial class LoadCmrView : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private LoadCmrViewModel ViewModel { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;

    private PhUploadFiles? _uploadFiles;

    protected override async Task OnInitializedAsync()
    {
        await ViewModel.LoadAsync();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private async Task OnCmrFileSelected(InputFileChangeEventArgs arg)
    {
        var result = await ViewModel.UploadCmrFileAsync(arg.File);

        if (result is { Status: not CmrTestOrderStatus.Ok })
        {
            Modal.ShowInformation(
                message: result.Status.ToLocalizedString("LoadCmr_Message"),
                ok: Loc["LoadCmr_Error_CloseButton"].Value,
                options: new ModalOptions
                {
                    Title = result.Status.ToLocalizedString("LoadCmr_Title"),
                    Status = ModalStatus.Warning,
                    ModalHeight = ModalSize.Small,
                    ModalWidth = ModalSize.Small
                });
        }
    }

    private async Task OpenFileDialog(MouseEventArgs arg)
    {
        if (_uploadFiles is not null)
        {
            await _uploadFiles.OpenFileDialog();
        }
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
