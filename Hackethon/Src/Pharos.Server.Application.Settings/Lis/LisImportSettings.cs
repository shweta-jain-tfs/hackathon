using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

/// <summary>
/// Protocol-independent data import settings for a LIS connection.
/// </summary>
public class LisImportSettings
{
    public LisImportSettings() { }

    public LisImportSettings(bool intervalsAreMilliseconds)
    {
        IntervalsAreMilliseconds = intervalsAreMilliseconds;
    }

    /// <summary>
    /// This is for testing purposes only. Read-only properties are not visible in the settings UI.
    /// </summary>
    public bool IntervalsAreMilliseconds { get; }


    [DisplayNameLocal("LisImportSettings_ForceUniqueLaboratoryTestNames")]
    public bool ForceUniqueLaboratoryTestNames { get; set; }

    [DisplayNameLocal("LisImportSettings_PollForRequests")]
    public bool PollForRequests { get; set; }

    /// <summary>
    /// Gets or sets the frequency for polling requests in minutes.
    /// </summary>
    [DisplayNameLocal("LisImportSettings_Frequency")]
    [DependsFrom(nameof(PollForRequests), "True", DependencyType.Disable)]
    [SettingsRange(1, 1000)]
    public int Frequency { get; set; } = 5;

    [DisplayNameLocal("LisImportSettings_AskForMissingRequestsInstrumentScan")]
    public bool AskForMissingRequestsInstrumentScan { get; set; }

    /// <summary>
    /// Gets or sets the timeout for missing requests in minutes.
    /// </summary>
    [DisplayNameLocal("LisImportSettings_Timeout")]
    [SettingsRange(1, 1000)]
    [DependsFrom(nameof(AskForMissingRequestsInstrumentScan), "True", DependencyType.Disable)]
    public int Timeout { get; set; } = 5;

    [DisplayNameLocal("LisImportSettings_AskForMissingRequestsManualScan")]
    public bool AskForMissingRequestsManualScan { get; set; }

    [DisplayNameLocal("LisImportSettings_RefreshExistingRequestsInstrumentScan")]
    public bool RefreshExistingRequestsInstrumentScan { get; set; }

    [DisplayNameLocal("LisImportSettings_RefreshExistingRequestsManualScan")]
    public bool RefreshExistingRequestsManualScan { get; set; }

    [DisplayNameLocal("LisImportSettings_ManualQueryForAll")]
    public bool ManualQueryForAll { get; set; } = true;

    [DisplayNameLocal("LisImportSettings_ManualRefreshExistingRequests")]
    public bool ManualRefreshExistingRequests { get; set; }

    [DisplayNameLocal("LisImportSettings_PanelExpansion")]
    public bool PanelExpansion { get; set; } = true;

    [DisplayNameLocal("LisImportSettings_AutomaticPanelRecognition")]
    public AutomaticPanelRecognition AutomaticPanelRecognition { get; set; } = AutomaticPanelRecognition.NotUsed;

    [DisplayNameLocal("LisImportSettings_RequestPriorityOrder")]
    public RequestPriorityOrder RequestPriorityOrder { get; set; } = RequestPriorityOrder.NotUsed;

    [DisplayNameLocal("LisImportSettings_CalculatePatientAge")]
    public bool CalculatePatientAge { get; set; }
}

public enum AutomaticPanelRecognition
{
    [DisplayNameLocal("LisImportSettings_AutomaticPanelRecognition_NotUsed")]
    NotUsed = 0,

    [DisplayNameLocal("LisImportSettings_AutomaticPanelRecognition_OnlyFromOneMessage")]
    OnlyFromOneMessage = 1,

    [DisplayNameLocal("LisImportSettings_AutomaticPanelRecognition_FromMultipleMessages")]
    FromMultipleMessages = 2
}

public enum RequestPriorityOrder
{
    [DisplayNameLocal("LisImportSettings_RequestPriorityOrder_NotUsed")]
    NotUsed = 0,

    [DisplayNameLocal("LisImportSettings_RequestPriorityOrder_UseOrderNumberA")]
    UseOrderNumberA = 1,

    [DisplayNameLocal("LisImportSettings_RequestPriorityOrder_UseOrderNumberB")]
    UseOrderNumberB = 2
}