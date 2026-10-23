using Microsoft.JSInterop;
using Pharos.Client.Common.App.Services;
using Pharos.Common.Web.Components.Icons;
using SettingsActionKey = Pharos.Client.Common.Models.ActionKey;

namespace Pharos.Client.App.Settings.Actions;

internal sealed class ImportSettingsAction(
    SettingsMainViewModel viewModel,
    SettingsActionOperationState operationState,
    UiSettingsService uiSettingsService) : ISettingsAction
{
    private static readonly SettingsActionScript _uploadScript = new("upload.js", SettingsActionScriptSource.SettingsModule);
    private static readonly SettingsActionConfirmation _confirmation = new(SettingsActionConfirmationType.DeleteExistingSettings);

    public SettingsActionKey ActionKey => SettingsActionKey.Import;

    public IconType? StartIconType => IconType.Download;

    public IReadOnlyCollection<SettingsActionScript> RequiredScripts { get; } = [_uploadScript];

    public SettingsActionConfirmation? Confirmation => _confirmation;

    public async Task<SettingsActionResult> ExecuteAsync(IReadOnlyDictionary<SettingsActionScript, IJSObjectReference> scripts)
    {
        if (!scripts.TryGetValue(_uploadScript, out var uploadScript))
        {
            return SettingsActionResult.Empty;
        }

        if (!operationState.TryBegin())
        {
            return SettingsActionResult.Empty;
        }

        try
        {
            var fileResult = await uploadScript.InvokeAsync<string>("uploadFileSelectedWithDialog");
            if (string.IsNullOrEmpty(fileResult))
            {
                return SettingsActionResult.From(new SettingsActionImportResultDialogEffect(
                    true,
                    [new FailedSettingImportError { Error = SettingImportError.InvalidFileType }],
                    false,
                    []));
            }

            var importResult = await viewModel.ImportSettings(new ImportSettingsCommand { CsvContent = fileResult });
            var effects = new List<SettingsActionEffect>();

            if (importResult.Success && importResult.Errors.Count == 0)
            {
                effects.Add(new SettingsActionToastEffect(SettingsActionToastType.Information, "SettingsImport_Successful_InfoToaster"));
                await uiSettingsService.NotifySettingsChangedAsync();
                if (importResult.UnrecognizedSettings.Count > 0)
                {
                    effects.Add(new SettingsActionImportResultDialogEffect(false, [], true, [.. importResult.UnrecognizedSettings]));
                }

                effects.Add(new SettingsActionNavigateToSettingsRootEffect());
            }
            else
            {
                var isErrorMalformedFile = importResult.Errors.Any(e => e.Error == SettingImportError.MalformedFile);
                effects.Add(new SettingsActionImportResultDialogEffect(isErrorMalformedFile, [.. importResult.Errors], false, []));
            }
            return new SettingsActionResult(effects);
        }
        finally
        {
            operationState.End();
        }
    }
}