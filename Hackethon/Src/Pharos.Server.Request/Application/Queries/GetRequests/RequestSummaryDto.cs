using Pharos.Server.Domain.Entities;
using Pharos.Server.Request.Domain;

#pragma warning disable CS8618

namespace Pharos.Server.Request.Application.Queries.GetRequests;

public class RequestSummaryDto
{
    public int Id { get; set; }
    public RequestUid RequestUid { get; set; } = default!;

    /// <summary>
    /// The friendly display name of the instrument(s) (resolved in AttachExtraInformation).
    /// </summary>
    public string? InstrumentName { get; set; }
    
    public List<RequestIssueDto> Issues { get; set; }
    public string? LastTray { get; set; }
    public string? LastTrayId { get; set; }
    public int? LastTrayTubePosition { get; set; }
    public bool HasMultipleTubes { get; set; }
    public string? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? RequestorId { get; set; }
    public string? RequestorName { get; set; }

    public DateTimeOffset? SampleCollectionDate { get; set; }
    public string SampleId { get; set; }
    public string SampleIdDisplay { get; set; }
    public RequestState State { get; set; }
    
    //TODO, remove property when external filter is implemented
    public bool HasOnHoldTests { get; set; }
    public RequestIssueType IssueTemp { get; set; }
}

public class RequestIssueDto
{
    public RequestIssueType IssueType { get; set; }
    public RequestIssueUrgency Urgency { get; set; }
    public string? Key { get; set; }
}