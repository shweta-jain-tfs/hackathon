using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Commands.RequestIssue;

internal record DeleteRequestIssueCommand(int RequestId, RequestIssueType IssueType, string? Key = null) : IRequest;

internal class DeleteRequestIssueCommandHandler : IRequestHandler<DeleteRequestIssueCommand>
{
    private readonly IRequestDbContext _dbContext;
    private readonly ISender _sender;
    private readonly IRequestHub _hub;

    public DeleteRequestIssueCommandHandler(IRequestDbContext dbContext, ISender sender, IRequestHub hub)
    {
        _dbContext = dbContext;
        _sender = sender;
        _hub = hub;
    }

    public async Task Handle(DeleteRequestIssueCommand request, CancellationToken cancellationToken)
    {
        var numberOfDeletedItems = await _dbContext.Issues.Where(x => x.RequestId == request.RequestId && x.IssueType == request.IssueType).ExecuteDeleteAsync(cancellationToken);
        if (numberOfDeletedItems > 0)
        {
            await _hub.Updated(request.RequestId);

            if (RequestIssueHelper.IssueHasTaskCard(request.IssueType))
            {
                await _sender.Send(new RegenerateRequestIssueTaskCardsCommand(), cancellationToken);
            }
        }
    }
}