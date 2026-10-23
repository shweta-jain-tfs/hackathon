
using System.ComponentModel;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.ViewModels;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.UploadFile;

namespace Pharos.Client.App.ServiceTools.Views.FlrImport;

public partial class FlrImportView : ComponentBase, IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private FlrImportViewModel ViewModel { get; set; } = null!;

    private PhUploadFiles? _uploadFiles;

    public void Dispose()
    {
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        await ViewModel.LoadAsync();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private async Task OnFlrFileChange(InputFileChangeEventArgs arg)
    {
        await ViewModel.StartImportAsync(arg.File);
    }

    private async Task OpenFileDialog(MouseEventArgs arg)
    {
        if (_uploadFiles is not null)
        {
            await _uploadFiles.OpenFileDialog();
        }
    }
}