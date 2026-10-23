using Microsoft.JSInterop;
using Pharos.Client.App.Parameters.Common.ViewModels;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.UploadFile;
using Pharos.Common.Web.Extensions;

namespace Pharos.Client.App.Parameters.Common.Views;

public partial class ParameterImportExportView
{
    [Inject] private ExportParameterPackageViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IParametersNotificationProvider NotificationProvider { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;

    private string ImportPackageDescriptionText { get; set; } = "";
    private bool ImportButtonDisabled { get; set; }

    private bool _isExporting;
    private IJSObjectReference? _script;
    private PhUploadFiles? _uploadFiles;

    protected override void OnInitialized()
    {
        NotificationProvider.ParameterUpdateFinished += OnParameterUpdateFinished;
    }
    private void OnParameterUpdateFinished(object? sender, bool e)
    {
        ImportButtonDisabled = false;
        StateHasChanged();
    }

    protected override async void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _script = await ImportCommonUtilityJSAsync("download.js");
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await _script.SafeDisposeAsync();
        NotificationProvider.ParameterUpdateFinished -= OnParameterUpdateFinished;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Export the package as a zip file.
    /// </summary>
    /// <returns></returns>
    private async Task ExportPackageAsZip()
    {
        if (_isExporting || _script is null)
        {
            return;
        }

        _isExporting = true;
        try
        {
            var file = await ViewModel.ExportPackageToZip();
            var fileName = file.Name;

            using var streamRef = new DotNetStreamReference(file.Stream);
            await _script.InvokeVoidAsync("downloadFromStreamReferenceWithDialog", fileName, streamRef, "zip");
        }
        finally
        {
            _isExporting = false;
        }
    }

    /// <summary>
    /// Import a package from a file. Do not allow if an update is already in progress.
    /// </summary>
    /// <returns></returns>
    private async Task ImportPackage_OpenFileDialog()
    {
        if (_uploadFiles is not null)
        {
            //check if update is already in progress
            if (await ViewModel.IsParameterJobInProgress())
            {
                ShowNotAllowedModal();
            }
            else
            {
                await _uploadFiles.OpenFileDialog();
            }
        }
    }

    /// <summary>
    /// Upload the file to the server.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    private async Task ImportPackage_UploadFile(InputFileChangeEventArgs e)
    {
        //check if update is already in progress
        if (await ViewModel.IsParameterJobInProgress())
        {
            ShowNotAllowedModal();
            return;
        }

        var file = e.File;

        await using var fileStream = file.OpenReadStream(file.Size);
        var uploadedPackageId = await ViewModel.UploadMethodUpdatePackageFileAsync(new FileParameter(fileStream, file.Name));

        ImportPackageDescriptionText = await ViewModel.GetMethodUpdatePackageDescriptionAsync(uploadedPackageId);

        if (string.IsNullOrEmpty(ImportPackageDescriptionText))
        {
            var modal = Modal.Show<ImportPackageErrorView>(new ModalOptions
            {
                Title = Loc["MethodsPage_ImportPackage_Error_Title"]
            });
            await modal.Result;
        }
        else
        {
            var modal = Modal.Show<ImportPackageView>(
                new ModalOptions
                {
                    Title = Loc["MethodsPage_ImportPackage_Confirm_Title"]
                },
                new ModalParameters
                {
                    { nameof(ImportPackageView.ImportPackageDescriptionText), ImportPackageDescriptionText }
                });

            var result = await modal.Result;

            if (result.Canceled)
            {
                await ImportPackage_Abort();
            }
            else
            {
                await ImportPackage_Confirm();
            }
        }
    }

    private void ShowNotAllowedModal()
    {
        var message = Loc["MethodsPage_ImportPackage_NotAllowed_Body"];
        var opt = new ModalOptions
        {
            Title = Loc["MethodsPage_ImportPackage_NotAllowed_Title"],
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small,
            Status = ModalStatus.Info,
        };
        var closeButtonText = Loc["MethodsPage_ImportPackage_NotAllowed_Ok"];
        Modal.ShowInformation(message, closeButtonText, opt, true);
    }

    /// <summary>
    /// Abort the import process.
    /// </summary>
    /// <returns></returns>
    private async Task ImportPackage_Abort()
    {
        await ViewModel.AbortPackageImport();
    }

    /// <summary>
    /// Confirm the import process.
    /// </summary>
    /// <returns></returns>
    private async Task ImportPackage_Confirm()
    {
        ImportButtonDisabled = true;
        await ViewModel.StartPackageImport();
    }
}