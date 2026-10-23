using System.Globalization;
using System.Text;
using Microsoft.JSInterop;
using Pharos.Common.Web.Components.Icons;
using SettingsActionKey = Pharos.Client.Common.Models.ActionKey;

namespace Pharos.Client.App.Settings.Actions;

internal sealed class ExportSettingsAction(
    SettingsMainViewModel viewModel,
    SettingsActionOperationState operationState) : ISettingsAction
{
    private static readonly SettingsActionScript _downloadScript = new("download.js", SettingsActionScriptSource.CommonWeb);

    public SettingsActionKey ActionKey => SettingsActionKey.Export;

    public IconType? StartIconType => IconType.Upload;

    public IReadOnlyCollection<SettingsActionScript> RequiredScripts { get; } = [_downloadScript];

    public SettingsActionConfirmation? Confirmation => null;

    public async Task<SettingsActionResult> ExecuteAsync(IReadOnlyDictionary<SettingsActionScript, IJSObjectReference> scripts)
    {
        if (!scripts.TryGetValue(_downloadScript, out var downloadScript))
        {
            return SettingsActionResult.Empty;
        }

        if (!operationState.TryBegin())
        {
            return SettingsActionResult.From(new SettingsActionExportResultDialogEffect("SettingsExportError_OngoingOperation"));
        }

        try
        {
            var fileName = $"Pharos_settings_{DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CurrentUICulture)}.csv";
            var confirmed = await downloadScript.InvokeAsync<bool>("checkExportDialog", fileName);

            if (!confirmed)
            {
                return SettingsActionResult.Empty;
            }

            var content = await viewModel.ExportSettings();
            if (string.IsNullOrEmpty(content))
            {
                return SettingsActionResult.From(new SettingsActionToastEffect(SettingsActionToastType.Warning, "SettingsExportError_NoSettingsFound"));
            }

            var byteArray = Encoding.UTF8.GetBytes(content);
            using var memoryStream = new MemoryStream(byteArray);
            using var streamRef = new DotNetStreamReference(stream: memoryStream);

            await downloadScript.InvokeVoidAsync("exportIfSavePressed", fileName, streamRef);
            return SettingsActionResult.From(new SettingsActionToastEffect(SettingsActionToastType.Information, "SettingsExport_Successful_InfoToaster"));
        }
        catch
        {
            return SettingsActionResult.From(new SettingsActionToastEffect(SettingsActionToastType.Warning, "SettingsExportError_InternalError"));
        }
        finally
        {
            operationState.End();
        }
    }
}