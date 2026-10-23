using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Commands.RequestIssue;

internal static class RequestIssueHelper
{
    public static bool IssueHasTaskCard(RequestIssueType issueType)
    {
        return issueType switch
        {
            RequestIssueType.TubeError => true,
            RequestIssueType.TestsRemaining => true,
            RequestIssueType.TestsRemainingLoadTube => true,
            RequestIssueType.MissingTestOrders => true,
            RequestIssueType.NotReported => true,
            _ => false
        };
    }
}