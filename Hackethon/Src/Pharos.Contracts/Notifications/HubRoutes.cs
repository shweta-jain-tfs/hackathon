namespace Pharos.Contracts.Notifications;

/// <summary>
/// Lists all available routes of client-server notification hubs in Pharos.
/// </summary>
public static class HubRoutes
{
    private const string _hubPrefix = "hubs/";

    public const string CurrentInstrument = _hubPrefix + "instrument/current";
    public const string CurrentUser = _hubPrefix + "user";
    public const string Job = _hubPrefix + "jobs";
    public const string Health = _hubPrefix + "health";
    public const string Instrument = _hubPrefix + "instrument";
    public const string Inventory = _hubPrefix + "inventory";
    public const string LabCommunity = _hubPrefix + "labcommunity";
    public const string Lis = _hubPrefix + "lis";
    public const string LoadList = _hubPrefix + "loadlist";
    public const string Maintenance = _hubPrefix + "maintenance";
    public const string Pairing = _hubPrefix + "pairing";
    public const string Result = _hubPrefix + "result";
    public const string Request = _hubPrefix + "request";
    public const string Reflex = _hubPrefix + "reflex";
    public const string Run = _hubPrefix + "run";
    public const string SampleTray = _hubPrefix + "sampleTray";
    public const string SampleTube = _hubPrefix + "sampleTube";
    public const string QualityControlTube = _hubPrefix + "qualityControlTube";
    public const string Settings = _hubPrefix + "settings";
    public const string SystemError = _hubPrefix + "systemError";
    public const string SystemUpdate = _hubPrefix + "systemUpdate";
    public const string TubeDefinition = _hubPrefix + "tubeDefinition";
    public const string TaskCard = _hubPrefix + "taskcard";
    public const string BarcodeFormat = _hubPrefix + "barcodeFormat";
}
