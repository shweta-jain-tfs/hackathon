using MediatR;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Commands.RequestIssue;

internal record RegenerateRequestIssueTaskCardsCommand : IRequest;

internal class RegenerateRequestIssueTaskCardsCommandHandler : IRequestHandler<RegenerateRequestIssueTaskCardsCommand>
{
    private readonly IRequestDbContext _dbContext;
    private readonly ISender _sender;

    private static readonly List<RequestIssueType> _sampleIssueTaskCard =
    [
        RequestIssueType.TubeError,
        RequestIssueType.TestsRemaining,
        RequestIssueType.TestsRemainingLoadTube,
        RequestIssueType.MissingTestOrders
    ];

    private static readonly List<RequestIssueType> _reportResultTaskCard = [RequestIssueType.NotReported];

    private static readonly Dictionary<RequestIssueUrgency, TaskCardState> _urgencyToTaskCardState = new()
    {
        { RequestIssueUrgency.Information, TaskCardState.Information },
        { RequestIssueUrgency.Warning, TaskCardState.Warning }
    };

    public RegenerateRequestIssueTaskCardsCommandHandler(IRequestDbContext dbContext, ISender sender)
    {
        _dbContext = dbContext;
        _sender = sender;
    }

    public async Task Handle(RegenerateRequestIssueTaskCardsCommand request, CancellationToken cancellationToken)
    {
        var mostSevereSampleIssue = await _dbContext.Issues.Where(x => _sampleIssueTaskCard.Contains(x.IssueType)).OrderBy(x => x.Urgency).FirstOrDefaultAsync(cancellationToken);
        var mostSevereReportResult = await _dbContext.Issues.Where(x => _reportResultTaskCard.Contains(x.IssueType)).OrderBy(x => x.Urgency).FirstOrDefaultAsync(cancellationToken);

        if (mostSevereSampleIssue == null)
        {
            await _sender.Send(new RemoveTaskCardCommand(TaskCardType.RequestSampleIssue), cancellationToken);
        }
        else
        {
            await _sender.Send(new TaskCardCommand(TaskCardType.RequestSampleIssue, _urgencyToTaskCardState[mostSevereSampleIssue.Urgency]), cancellationToken);
        }

        if (mostSevereReportResult == null)
        {
            await _sender.Send(new RemoveTaskCardCommand(TaskCardType.RequestReportResult), cancellationToken);
        }
        else
        {
            await _sender.Send(new TaskCardCommand(TaskCardType.RequestReportResult, _urgencyToTaskCardState[mostSevereReportResult.Urgency]), cancellationToken);
        }
    }
}