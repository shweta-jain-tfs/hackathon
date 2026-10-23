using MediatR;
using Pharos.Server.Application.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Events;

internal class QualityControlTestResultSentToLisEventHandler(IResultDbContext dbContext) : INotificationHandler<QualityControlTestResultSentToLisEvent>
{
    public async Task Handle(QualityControlTestResultSentToLisEvent notification, CancellationToken cancellationToken)
    {
        var testResult = await dbContext.QualityControlTestResults.FirstOrDefaultAsync(x => x.Id == notification.TestResultId, cancellationToken);
        if (testResult != null)
        {
            testResult.Status = TestResultStatus.Reported;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
