namespace Pharos.Server.Application.Queries;

public class SearchResultDto
{
    public int Id { get; set; }
    public string Match { get; set; } = string.Empty;
    public SearchResultType ResultType { get; set; } = SearchResultType.Unknown;
    public string? StringId { get; set; }
    public SearchResultModules Module { get; set; }
    /// <summary>
    /// Used for describing the destination of the result, e.g. a sample ID if you found a request based on requestor ID.
    /// </summary>
    public string? DestinationHint { get; set; }
}

public enum SearchResultModules
{
    Request = 0,
    Settings = 1,
    Parameters = 2,
}

public enum SearchResultType
{
    Unknown,
    ArticleIdentity,
    ArticleFullName,
    RequestorId,
    RequestorName,
    RequestId,
    SampleId,
    PatientId,
    Setting, // unspecified settings section
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
}
