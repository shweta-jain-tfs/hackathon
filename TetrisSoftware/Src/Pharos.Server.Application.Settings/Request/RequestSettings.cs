using Pharos.Contracts.Security;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Request;

[RootConfigSection(sortOrder: 30)]
[DisplayNameLocal("RequestSettings")]
public class RequestSettings
{
    [DisplayNameLocal("RequestSettings_SampleVolumeCheck")]
    public SampleVolumeCheckSettings SampleVolumeCheckSettings { get; set; } = new();

    /// <summary>
    /// Seconds until missing test order event is raised after a sample tube has been identified and sent to LIS
    /// </summary>
    [DisplayNameLocal("RequestSettings_AllowEditingOfPredilutionFactor")]
    public bool AllowEditingOfPredilutionFactor { get; set; } = false;

    /// <summary>
    /// Enables the use of reusing a sample id
    /// </summary>
    [DisplayNameLocal("RequestSettings_ReuseSampleId")]
    public bool ReuseSampleId { get; set; } = false;

    /// <summary>
    /// Enables the use of reusing a sample id
    /// </summary>
    [DisplayNameLocal("RequestSettings_DaysBeforeReuseOfSampleId")]
    [DependsFrom(nameof(ReuseSampleId), "True", DependencyType.Hide)]
    [SettingsRange(1, 40000)]
    public int DaysBeforeReuseOfSampleId { get; set; } = 365;

    /// <summary>
    /// Enables the use of requestor information
    /// </summary>
    [Permission(Permissions.EditTestSettings)]
    [DisplayNameLocal("RequestSettings_IsRequestorInformationEnabled")]
    public bool IsRequestorInformationEnabled { get; set; } = false;

    /// <summary>
    /// Enables the use of patient information
    /// </summary>
    [Permission(Permissions.EditTestSettings)]
    [DisplayNameLocal("RequestSettings_IsPatientInformationEnabled")]
    public bool IsPatientInformationEnabled { get; set; } = false;

    /// <summary>
    /// Enables the use of laboratory test names
    /// </summary>
    [DisplayNameLocal("RequestSettings_UseLaboratoryTestNames")]
    public bool UseLaboratoryTestNames { get; set; } = false;
    
    /// <summary>
    /// If the reflex testing is not enabled, the reflex will be hidden from parameters
    /// </summary>
    [DisplayNameLocal("RequestSettings_ReflexTestingEnabled")]
    public bool ReflexTestingEnabled { get; set; } = true;

    /// <summary>
    /// Prevent adding reflex tests when the same method and test already exist on the request.
    /// </summary>
    [DisplayNameLocal("RequestSettings_PreventDuplicateReflexTests")]
    public bool PreventDuplicateReflexTests { get; set; } = false;
}

public class SampleVolumeCheckSettings
{
    [DisplayNameLocal("SampleVolumeCheckSettings_UseSampleVolumeCheck")]
    public bool UseSampleVolumeCheck { get; set; } = false;

    [DisplayNameLocal("SampleVolumeCheckSettings_SelectedSampleVolumeCheck")]
    [DependsFrom(nameof(UseSampleVolumeCheck), "True", DependencyType.Hide)]
    public SettingsSampleVolumeCheck SelectedSampleVolumeCheck { get; set; } = SettingsSampleVolumeCheck.Off;

    [DisplayNameLocal("SampleVolumeCheckSettings_OnlyForImmunoCap")]
    [DependsFrom(nameof(UseSampleVolumeCheck), "True", DependencyType.Hide)]
    public bool OnlyForImmunoCap { get; set; } = false;

    [DisplayNameLocal("SampleVolumeCheckSettings_UseSampleVolumeCheckPediatric")]
    public bool UseSampleVolumeCheckPediatric { get; set; } = false;

    [DisplayNameLocal("SampleVolumeCheckSettings_Age")]
    [DependsFrom(nameof(UseSampleVolumeCheckPediatric), "True", DependencyType.Hide)]
    [SettingsRange(0, 100)]
    public int Age { get; set; } = 3;

    [DisplayNameLocal("SampleVolumeCheckSettings_SelectedSampleVolumeCheck")]
    [DependsFrom(nameof(UseSampleVolumeCheckPediatric), "True", DependencyType.Hide)]
    public SettingsSampleVolumeCheckPediatric SelectedSampleVolumeCheckPediatric { get; set; } = SettingsSampleVolumeCheckPediatric.OnRunNoTestsWhenVolumeTooLow;
}

public enum SettingsSampleVolumeCheck
{
    [DisplayNameLocal("SettingsSampleVolumeCheck_Off")]
    Off = 0,
    [DisplayNameLocal("SettingsSampleVolumeCheck_OnRunAccordingToTestOrder")]
    OnRunAccordingToTestOrder = 1,
    [DisplayNameLocal("SettingsSampleVolumeCheck_OnRunNoTestsWhenVolumeTooLow")]
    OnRunNoTestsWhenVolumeTooLow = 2
}

public enum SettingsSampleVolumeCheckPediatric
{
    [DisplayNameLocal("SettingsSampleVolumeCheck_OnRunAccordingToTestOrder")]
    OnRunAccordingToTestOrder = 1,
    [DisplayNameLocal("SettingsSampleVolumeCheck_OnRunNoTestsWhenVolumeTooLow")]
    OnRunNoTestsWhenVolumeTooLow = 2
}