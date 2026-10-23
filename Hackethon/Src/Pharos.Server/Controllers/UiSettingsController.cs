using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.General;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Controllers.Dto;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Controllers;

[ApiController]
[Route("api/uisettings")]
public class UiSettingsController(
    IOptionsSnapshot<GeneralSettings> generalSettings,
    IOptionsSnapshot<SecuritySettings> identitySettings,
    IOptionsSnapshot<RequestSettings> requestSettings,
    IOptionsSnapshot<SpecialSampleHandlingSettings> specialSampleHandling,
    IOptionsSnapshot<LisSettings> lisSettings,
    IOptionsSnapshot<SamplesSettings> samplesSettings,
    IOptionsSnapshot<TubeTypesSettings> tubeTypesSettings) : ControllerBase
{
    private readonly GeneralSettings _generalSettings = generalSettings.Value;
    private readonly SecuritySettings _identitySettings = identitySettings.Value;
    private readonly RequestSettings _requestSettings = requestSettings.Value;
    private readonly SpecialSampleHandlingSettings _specialSampleHandling = specialSampleHandling.Value;
    private readonly LisSettings _lisSettings = lisSettings.Value;
    private readonly SamplesSettings _samplesSettings = samplesSettings.Value;
    private readonly TubeTypesSettings _tubeTypesSettings = tubeTypesSettings.Value;

    [HttpGet]
    public ActionResult<UiSettingsDto> GetUiSettings()
    {
        var allSettings = new UiSettingsDto
        {
            SecuritySettings = new SecuritySettingsDto
            {
                PasswordRequireDigit = _identitySettings.PasswordComplexitySettings.RequireDigit,
                PasswordRequireNonAlphanumeric = _identitySettings.PasswordComplexitySettings.RequireNonAlphanumeric,
                PasswordRequiresBothUpperAndLowerCase = _identitySettings.PasswordComplexitySettings.RequireBothUpperAndLowerCase,
                PasswordRequiredLength = _identitySettings.PasswordComplexitySettings.RequiredLength,
                PasswordRequireNoUsername = _identitySettings.PasswordComplexitySettings.RequireNoUsernameInPassword,
                PasswordExpirationEnabled = _identitySettings.PasswordExpirationEnabled,
                EnablePinLogin = _identitySettings.EnablePinLogin,
                AutoLogoutEnabled = _identitySettings.AutoLogoutSettings.AutoLogoutEnabled,
                AutoLogoutTimeoutMinutes = _identitySettings.AutoLogoutSettings.AutoLogoutTimeoutMinutes
            },

            RegionalSettings = new RegionalSettingsDto
            {
                Culture = _generalSettings.RegionalSettings.Culture.ToCultureName(),
                TemperatureUnit = _generalSettings.RegionalSettings.TemperatureUnit
            },

            RequestSettings = new RequestSettingsDto
            {
                AllowEditingOfPredilutionFactor = _requestSettings.AllowEditingOfPredilutionFactor,
                IsPatientInformationEnabled = _requestSettings.IsPatientInformationEnabled,
                IsRequestorInformationEnabled = _requestSettings.IsRequestorInformationEnabled,
                ShowTubeId = _specialSampleHandling.ShowTubeId(),
                ManualQueryForAll = _lisSettings.LisChannels
                    .Where(channel => channel.IsEnabled)
                    .Any(channel => channel.ImportSettings.ManualQueryForAll),
                ManualRefreshExistingRequests = _lisSettings.LisChannels
                    .Where(channel => channel.IsEnabled)
                    .Any(channel => channel.ImportSettings.ManualRefreshExistingRequests),
                ReflexTestingEnabled = _requestSettings.ReflexTestingEnabled,
                ManualExportRequests = _lisSettings.LisChannels
                    .Any(channel => channel.IsEnabled),
                DefaultTubeTypeId = int.TryParse(_tubeTypesSettings.NormalTubeDefinitionId, out var id) ? id : null,
                DefaultSampleVolumeCheck = Map(_requestSettings.SampleVolumeCheckSettings)
            },
            
            SamplesSettings = new SamplesSettingsDto
            {
                IsLeftDrawerAsInputAndRightDrawerAsOutput = _samplesSettings.Drawer.DrawerMode == DrawerMode.LeftDrawerAsInputAndRightDrawerAsOutput
            }

        };
        return Ok(allSettings);
    }

    private static SampleVolumeCheck Map(SampleVolumeCheckSettings settings)
    {
        if(!settings.UseSampleVolumeCheck)
        {
            return SampleVolumeCheck.Off;
        }

        return settings.SelectedSampleVolumeCheck switch
        {
            SettingsSampleVolumeCheck.Off => SampleVolumeCheck.Off,
            SettingsSampleVolumeCheck.OnRunAccordingToTestOrder => SampleVolumeCheck.OnRunAccordingToTestOrder,
            SettingsSampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow => SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow,
            _ => throw new ArgumentOutOfRangeException(nameof(settings), settings, null)
        };
    }
}

public class UiSettingsDto
{
    public required SecuritySettingsDto SecuritySettings { get; set; }
    public required RegionalSettingsDto RegionalSettings { get; set; }
    public required RequestSettingsDto RequestSettings { get; set; }
    public required SamplesSettingsDto SamplesSettings { get; set; }
}


public class RegionalSettingsDto
{
    public required string Culture { get; set; }
    public required TemperatureUnit TemperatureUnit { get; set; }
}

public class RequestSettingsDto
{
    public bool AllowEditingOfPredilutionFactor { get; set; }
    public bool IsPatientInformationEnabled { get; set; }
    public bool IsRequestorInformationEnabled { get; set; }
    public bool ShowTubeId { get; set; }
    public bool ManualQueryForAll { get; set; }
    public bool ManualRefreshExistingRequests { get; set; }
    public bool ReflexTestingEnabled { get; set; }
    public bool ManualExportRequests { get; set; }
    public int? DefaultTubeTypeId { get; set; }
    public SampleVolumeCheck DefaultSampleVolumeCheck { get; set; }
}

public class SamplesSettingsDto
{
    public bool IsLeftDrawerAsInputAndRightDrawerAsOutput { get; set; }
}