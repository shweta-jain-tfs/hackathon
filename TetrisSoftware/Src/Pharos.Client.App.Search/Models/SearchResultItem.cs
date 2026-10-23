namespace Pharos.Client.App.Search.Models;

public sealed class SearchResultItem
{
    public int Id { get; set; }
    public string Match { get; set; } = string.Empty;
    public ClientSearchResultType ResultType { get; set; } = ClientSearchResultType.Unknown;
    public string? StringId { get; set; }
    public ClientSearchResultModule Module { get; set; } = ClientSearchResultModule.Unknown;
    public string? DestinationHint { get; set; }
}

public enum ClientSearchResultModule
{
    Unknown,
    Request,
    Settings,
    Parameters,

    // Client-only
    Help,
}

public enum ClientSearchResultType
{
    Unknown,
    ArticleIdentity,
    ArticleFullName,
    RequestorId,
    RequestorName,
    RequestId,
    SampleId,
    PatientId,
    Setting,
    SettingDatabase,
    SettingInstrument,
    SettingLabCommunity,
    SettingLis,
    SettingReagents,
    SettingRequests,
    SettingResult,
    SettingSamples,
    SettingSecurity,
    SettingTaskCard,

    // Client-only
    HelpChapter,
}
