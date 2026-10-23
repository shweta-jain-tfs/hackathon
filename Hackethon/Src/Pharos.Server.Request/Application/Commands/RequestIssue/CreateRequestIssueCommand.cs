using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Commands.RequestIssue;

internal record CreateRequestIssueCommand(int RequestId, RequestIssueType IssueType, string? Key = null) : IRequest;

internal class CreateRequestIssueCommandHandler : IRequestHandler<CreateRequestIssueCommand>
{
    private static readonly Dictionary<RequestIssueType, RequestIssueUrgency> _issueToUrgency = new()
    {
        { RequestIssueType.TubeError,                RequestIssueUrgency.Warning     },
        { RequestIssueType.TestsRemaining,           RequestIssueUrgency.Information },
        { RequestIssueType.TestsRemainingLoadTube,   RequestIssueUrgency.Information },
        { RequestIssueType.TestsRemainingMoveTube,   RequestIssueUrgency.Information },
        { RequestIssueType.MissingTestOrders,        RequestIssueUrgency.Information },
        { RequestIssueType.NotReported,              RequestIssueUrgency.Information }
    };

    private readonly IRequestDbContext _dbContext;
    private readonly ISender _sender;
    private readonly IRequestHub _hub;

    public CreateRequestIssueCommandHandler(IRequestDbContext dbContext, ISender sender, IRequestHub hub)
    {
        _dbContext = dbContext;
        _sender = sender;
        _hub = hub;
    }

    public async Task Handle(CreateRequestIssueCommand request, CancellationToken cancellationToken)
    {
        var requestExists = await _dbContext.Requests.AnyAsync(x => x.Id == request.RequestId, cancellationToken);

        if (!requestExists)
        {
            return;
        }

        var urgency = GetUrgency(request.IssueType);
        
        var existingIssue = await _dbContext.Issues.Where(x => x.RequestId == request.RequestId && x.IssueType == request.IssueType && x.Key == request.Key).FirstOrDefaultAsync(cancellationToken);
        if(existingIssue == null)
        {
            var issue = new Domain.RequestIssue
            {
                RequestId = request.RequestId,
                IssueType = request.IssueType,
                Urgency = urgency,
                Key = request.Key
            };
            _dbContext.Issues.Add(issue);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _hub.Updated(request.RequestId);

            if (RequestIssueHelper.IssueHasTaskCard(request.IssueType))
            {
                await _sender.Send(new RegenerateRequestIssueTaskCardsCommand(), cancellationToken);
            }
        }
    }

    private static RequestIssueUrgency GetUrgency(RequestIssueType issueType)
    {
        if (!_issueToUrgency.TryGetValue(issueType, out var urgency))
        {
            throw new ArgumentOutOfRangeException(nameof(issueType));
        }

        return urgency;
    }
}