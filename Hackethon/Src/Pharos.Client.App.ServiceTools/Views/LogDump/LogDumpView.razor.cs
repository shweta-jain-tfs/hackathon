using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Pharos.Client.App.ServiceTools.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;
using Pharos.Common.Web.Extensions;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.ServiceTools.Views.LogDump;

public partial class LogDumpView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private IModalService Modal { get; set; } = null!;
    [Inject] private IToasterService Toaster { get; set; } = null!;
    [Inject] public IAuthorizationService AuthorizationService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private LogDumpViewModel ViewModel { get; set; } = null!;

    private IJSObjectReference? _script;
    private bool IsAuthorized { get; set; }

    public override async ValueTask DisposeAsync()
    {
        await _script.SafeDisposeAsync();
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        IsAuthorized = await AuthorizationService.IsAuthorized(Permissions.EditSystemSettings);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        await ViewModel.LoadAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _script = await ImportCommonUtilityJSAsync("download.js");
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private async Task OpenCollectLogFilesAsync()
    {
        if (await ViewModel.IsCollectionInProgressAsync())
        {
            await ShowCollectionInProgressAsync();
            return;
        }

        var modal = Modal.Show<CollectLogFilesView>(new ModalOptions
        {
            Title = Loc["ServiceToolsPage_CollectLogFilesDialog_Title"],
            ModalHeight = ModalSize.XLarge,
            ModalWidth = ModalSize.Medium
        });

        var result = await modal.Result;
        if (!result.Canceled && result.Data is CollectLogFilesViewModel viewModel)
        {
            var collectionStarted = await ViewModel.StartCollectionAsync(viewModel.Request);
            if (collectionStarted)
            {
                Toaster.ShowToast(ToastType.Information, Loc["ServiceToolsPage_CollectLogFiles_Started"]);
            }
        }
    }

    private async Task ShowCollectionInProgressAsync()
    {
        var modal = Modal.ShowConfirm(
            Loc["ServiceToolsPage_CollectLogFiles_InProgressDialog_Body"],
            Loc["ServiceToolsPage_CollectLogFiles_InProgressDialog_Close"],
            Loc["ServiceToolsPage_CollectLogFiles_InProgressDialog_Abort"],
            new ModalOptions
            {
                Title = Loc["ServiceToolsPage_CollectLogFiles_InProgressDialog_Title"],
                Status = ModalStatus.Info,
                ModalWidth = ModalSize.Small,
                ModalHeight = ModalSize.Small
            });

        var result = await modal.Result;
        if (!result.Canceled && result.Data is true)
        {
            await ViewModel.CancelCollectionAsync();
        }
    }

    private async Task DownloadAsync()
    {
        if (_script is null)
        {
            return;
        }

        var fileName = ViewModel.FileName;
        var relativeFileUrl = $"api/logDump/export/{ViewModel.FileId}";
        var absoluteFileUrl = Navigation.ToAbsoluteUri(relativeFileUrl).ToString();

        await _script.InvokeVoidAsync("downloadFromUrl", fileName, absoluteFileUrl);
    }
}