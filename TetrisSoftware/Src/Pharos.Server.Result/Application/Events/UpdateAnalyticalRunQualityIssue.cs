using MediatR;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

internal record UpdateAnalyticalRunQualityIssue(AnalyticalRun Run, AnalyticalRunIssue? CurveIssue, AnalyticalRunIssue? CurveControlIssue, AnalyticalRunIssue? QualityControlIssue, AnalyticalRunIssue? TestIssue) : INotification;

internal sealed class UpdateAnalyticalRunQualityIssueHandler(
    IResultDbContext dbContext,
    IMediator mediator)
    : INotificationHandler<UpdateAnalyticalRunQualityIssue>
{
    public async Task Handle(UpdateAnalyticalRunQualityIssue notification, CancellationToken cancellationToken)
    {
        var analyticalRun = notification.Run;

        // you can only pass a deattached entity to a notification, because it runs outside the publishing scope
        dbContext.AnalyticalRuns.Attach(analyticalRun);

        var before = (
            CalibrationCurveIssue: analyticalRun.CalibrationCurveIssue,
            CurveControlIssue: analyticalRun.CurveControlIssue,
            QualityControlIssue: analyticalRun.QualityControlIssue,
            TestIssue: analyticalRun.TestIssue);

        analyticalRun.CalibrationCurveIssue = UpdateIssueIfHigherOrNoIssue(analyticalRun.CalibrationCurveIssue, notification.CurveIssue);
        analyticalRun.CurveControlIssue = UpdateIssueIfHigherOrNoIssue(analyticalRun.CurveControlIssue, notification.CurveControlIssue);
        analyticalRun.QualityControlIssue = UpdateIssueIfHigherOrNoIssue(analyticalRun.QualityControlIssue, notification.QualityControlIssue);
        analyticalRun.TestIssue = UpdateIssueIfHigherOrNoIssue(analyticalRun.TestIssue, notification.TestIssue);

        var after = (
            CalibrationCurveIssue: analyticalRun.CalibrationCurveIssue,
            CurveControlIssue: analyticalRun.CurveControlIssue,
            QualityControlIssue: analyticalRun.QualityControlIssue,
            TestIssue: analyticalRun.TestIssue);

        if (before == after)
        {
            return;
        }

        analyticalRun.AddDomainEvent(new AnalyticalRunUpdatedEvent(analyticalRun));
        await dbContext.SaveChangesAsync(cancellationToken);

        var affectedCardTypes = GetAffectedCardTypes(before, after);

        await ResultHelper.UpdateTaskCardState(mediator, dbContext, notification.Run.InstrumentNumber, affectedCardTypes, cancellationToken);

    }

    private static AnalyticalRunIssue UpdateIssueIfHigherOrNoIssue(AnalyticalRunIssue currentIssue, AnalyticalRunIssue? newIssue)
    {
        if (newIssue == null)
        {
            return currentIssue;
        }

        // Reset or update
        if (newIssue == AnalyticalRunIssue.NoIssue || newIssue > currentIssue)
        {
            return newIssue.Value;
        }

        return currentIssue;
    }

    private static List<TaskCardType> GetAffectedCardTypes(
        (AnalyticalRunIssue CalibrationCurveIssue, AnalyticalRunIssue CurveControlIssue, AnalyticalRunIssue QualityControlIssue, AnalyticalRunIssue TestIssue) before,
        (AnalyticalRunIssue CalibrationCurveIssue, AnalyticalRunIssue CurveControlIssue, AnalyticalRunIssue QualityControlIssue, AnalyticalRunIssue TestIssue) after)
    {
        var affectedCardTypes = new List<TaskCardType>(3);

        // Only add CalibrationIssue if Cal or CC has changed, since it is a separate card type
        if (before.CalibrationCurveIssue != after.CalibrationCurveIssue || before.CurveControlIssue != after.CurveControlIssue)
        {
            affectedCardTypes.Add(TaskCardType.CalibrationIssue);
        }

        // Only add QCIssue if it has changed, since it is a separate card type
        if (before.QualityControlIssue != after.QualityControlIssue)
        {
            affectedCardTypes.Add(TaskCardType.QCIssue);
        }

        // Only add ResultsIssue if it has changed, since it is a separate card type
        if (before.TestIssue != after.TestIssue)
        {
            affectedCardTypes.Add(TaskCardType.ResultsIssue);
        }

        return affectedCardTypes;
    }
}
