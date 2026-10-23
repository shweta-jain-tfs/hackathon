using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Events;

internal class TestOrdersDeletedEventHandler(IResultDbContext dbContext) : INotificationHandler<TestOrdersDeletedEvent>
{
    public async Task Handle(TestOrdersDeletedEvent notification, CancellationToken cancellationToken)
    {
        await dbContext.SampleTestResults.Where(x => notification.TestOrderIds.Contains(x.TestOrderId) && x.AnalyticalRunId == null).ExecuteDeleteAsync(cancellationToken);
    }
}