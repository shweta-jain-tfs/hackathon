using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Request.Domain;

#pragma warning disable CS8618
internal class RequestIssue : BaseEntity
{
    public RequestIssueType IssueType { get; set; }
    public RequestIssueUrgency Urgency { get; set; }

    /// <summary>
    /// If the issue is related to something specific "outside" of the request.
    /// The key when IssueType is TestsRemainingLoadTube is the instrument name, which is required to be non-empty
    /// The key when IssueType is TestsRemainingMoveTube should be in the format of "From_Instrument{char31}To_Instrument"
    /// </summary>
    [MaxLength(50)]
    public string? Key { get; set; }

    public int RequestId { get; set; }
    public virtual Request Request { get; set; }
}

public enum RequestIssueType
{
    TubeError = 0,
    TestsRemaining = 1,
    TestsRemainingLoadTube = 2,
    TestsRemainingMoveTube = 3,
    MissingTestOrders = 4,
    NotReported = 5
}

public enum RequestIssueUrgency
{
    Information = 0,
    Warning = 1
}