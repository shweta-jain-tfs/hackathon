using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Pharos.Common.Web.Extensions;

namespace Pharos.Common.Web.Components.UploadFile;

public partial class PhUploadFiles
{
    [Parameter] public bool AllowMultipleFiles { get; set; } = false;
    [Parameter] public EventCallback<InputFileChangeEventArgs> OnFileUploaded { get; set; }
    [Parameter] public string Accept { get; set; } = string.Empty;

    private IJSObjectReference? _domUtils;

    private InputFile? _inputFile;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _domUtils = await ImportCommonUtilityJSAsync("domUtils.js");
        }
    }

    /// <summary>
    /// Opens the file dialog.
    /// </summary>
    /// <returns></returns>
    public async Task OpenFileDialog()
    {
        if (_inputFile is null || _domUtils is null)
        {
            return;
        }

        await _domUtils.InvokeVoidAsync("click", _inputFile.Element);
    }

    /// <summary>
    /// Handles the file selection event.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    private async Task UploadFiles(InputFileChangeEventArgs e)
    {
        if (AllowMultipleFiles)
        {
            var files = e.GetMultipleFiles();
            await OnFileUploaded.InvokeAsync(new InputFileChangeEventArgs(files));
        }
        else
        {
            await OnFileUploaded.InvokeAsync(e);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await _domUtils.SafeDisposeAsync();

        GC.SuppressFinalize(this);
    }
}
