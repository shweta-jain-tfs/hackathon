namespace Pharos.Contracts.Notifications;

/// <summary>
/// Lists all available routes of client-server notification hubs in Pharos.
/// </summary>
public static class HubRoutes
{
    private const string _hubPrefix = "hubs/";

    public const string CurrentInstrument = _hubPrefix + "instrument/current";
    public const string CurrentInstrumentStorage = CurrentInstrument + "/storage";
    public const string Instrument = _hubPrefix + "instrument";
    public const string Lis = _hubPrefix + "lis";
    public const string Result = _hubPrefix + "result";
    public const string Sample = _hubPrefix + "sample";
    public const string SampleTray = _hubPrefix + "sampleTray";
    public const string Settings = _hubPrefix + "settings";
    public const string TubeDefinition = _hubPrefix + "tubeDefinition";
    public const string WorkTask = _hubPrefix + "worktask";
}
