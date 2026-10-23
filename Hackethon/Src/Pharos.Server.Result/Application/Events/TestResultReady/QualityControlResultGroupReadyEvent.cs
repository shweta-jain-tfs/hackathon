using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Mapping;
using Pharos.Server.Result.Application.Services;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events.TestResultReady;

/// <summary>
/// This event is published when all test results in a QC group have been received
/// and response and concentration values for all QC replicates have been calculated.
/// </summary>
internal record QualityControlResultGroupReadyEvent(string InstrumentNumber, Guid GroupId) : INotification;

internal sealed class QualityControlResultGroupReadyEventHandler(
    IResultDbContext dbContext,
    IMethodsService methodsService,
    IResultCalculationService resultCalculationService,
    IOptionsSnapshot<LisSettings> lisSettings,
    IMediator mediator,
    LisTestResultMapper mapper) : TestResultReadyEventHandlerBase<QualityControlTestResult>(dbContext), INotificationHandler<QualityControlResultGroupReadyEvent>
{
    public async Task Handle(QualityControlResultGroupReadyEvent notification, CancellationToken cancellationToken)
    {
        var testResult = await DbContext.QualityControlTestResults
            .Include(to => to.Replicates)
            .Where(to => to.InstrumentNumber == notification.InstrumentNumber && to.GroupId == notification.GroupId)
            .SingleAsync(cancellationToken);

        await resultCalculationService.CalculateSummaryResult(testResult);

        var (rangeMin, rangeMax, percentCvLimit) = await CalculateJudgement(testResult, cancellationToken);

        await SetTestResultStatus(testResult, cancellationToken);
        testResult.AddDomainEvent(new QualityControlChartUpdatedEvent(testResult.InstrumentNumber, testResult.MethodName));
        testResult.AddDomainEvent(new TestResultUpdatedEvent(testResult));

        await DbContext.SaveChangesAsync(cancellationToken);

        await EnsureReagentsFetched(testResult, cancellationToken);

        await mediator.Send(
            SendResultToLabCommunityCommandFactory.Create(testResult, rangeMin, rangeMax, percentCvLimit),
            cancellationToken);

        // Export quality control results to LIS (only those that have concentration)
        if (testResult.MeanConcentrationStatus == ConcentrationStatus.NotAvailable)
        {
            return;
        }

        var lisChannelId = await GetLisChannelIdByMethod(testResult.MethodName);
        if (!LisChannelNeedsQcResults(lisChannelId))
        {
            return;
        }

        var qcTestResultForLis = await mapper.ToLisTestResults([testResult]);
        await mediator.Send(new SendQualityControlTestResultToLisCommand(lisChannelId, qcTestResultForLis[0]), cancellationToken);
    }

    private async Task<(double RangeMin, double RangeMax, double PercentCvLimit)> CalculateJudgement(
        QualityControlTestResult qcTestResult,
        CancellationToken cancellationToken)
    {
        qcTestResult.Judgment = QualityControlJudgement.NotDefined;

        var (lowerLimit, upperLimit, excludeLowerLimit, excludeUpperLimit, replicateCvLimit) = await GetLimits(qcTestResult);

        switch (qcTestResult.QualityControlType)
        {
            case QualityControlType.Concentration or QualityControlType.Qualitative:
                {
                    var percentCv = CalculateConcentrationPercentCv(qcTestResult.Replicates);
                    JudgeAccordingToLimits(qcTestResult.MeanConcentration, percentCv, qcTestResult, lowerLimit, upperLimit, replicateCvLimit, excludeLowerLimit, excludeUpperLimit);
                    break;
                }
            case QualityControlType.Quotient or QualityControlType.DilutionControl:
                {
                    if (qcTestResult.Replicates != null)
                    {
                        var qcReplicates = qcTestResult.Replicates.Cast<QualityControlTestReplicate>().ToList();
                        var percentCv = CalculateQuotientPercentCv(qcReplicates);
                        JudgeAccordingToLimits(qcTestResult.MeanQuotient * 100.0, percentCv, qcTestResult, lowerLimit, upperLimit, replicateCvLimit, excludeLowerLimit, excludeUpperLimit);
                    }

                    break;
                }
            default:
                throw new NotImplementedException("QualityControlType not implemented: " + qcTestResult.QualityControlType.ToString());
        }

        await SaveQualityIssue(qcTestResult);

        // QC result produced from a tube and with a bad judgment should trigger a QualityControlFailedEvent to unload the tube from the instrument.
        if (!string.IsNullOrEmpty(qcTestResult.TubeId) && IsBadJudgment(qcTestResult.Judgment))
        {
            await mediator.Publish(new QualityControlFailedEvent(qcTestResult.TubeId!), cancellationToken);
        }

        return (lowerLimit ?? 0, upperLimit ?? 0, replicateCvLimit ?? 0);
    }

    private static bool IsBadJudgment(QualityControlJudgement judgment)
    {
        return judgment switch
        {
            QualityControlJudgement.NotDefined or QualityControlJudgement.Ok => false,
            QualityControlJudgement.FlaggedHigh or QualityControlJudgement.FlaggedLow or QualityControlJudgement.High or QualityControlJudgement.Low or QualityControlJudgement.HighCv => true,
            _ => throw new ArgumentOutOfRangeException(nameof(judgment), judgment, null)
        };
    }

    private async Task EnsureReagentsFetched(QualityControlTestResult qcTestResult, CancellationToken cancellationToken)
    {
        if (qcTestResult.Replicates == null || !qcTestResult.Replicates.Any()
            || qcTestResult.Replicates.Any(r => r.TestReplicateReagents == null || !r.TestReplicateReagents.Any()))
        {
            await DbContext.TestReplicates.Where(x => x.TestResultId == qcTestResult.Id)
                .Include(r => r.TestReplicateReagents)
                .ToListAsync(cancellationToken);
        }
    }

    private async Task SaveQualityIssue(QualityControlTestResult qcTestResult)
    {
        var qualityControlJudgement = qcTestResult.Judgment;

        var qcIssue = qualityControlJudgement switch
        {
            QualityControlJudgement.Ok => AnalyticalRunIssue.NoIssue,
            QualityControlJudgement.FlaggedHigh or QualityControlJudgement.FlaggedLow => AnalyticalRunIssue.Warning,
            QualityControlJudgement.NotDefined or QualityControlJudgement.High or QualityControlJudgement.Low or QualityControlJudgement.HighCv => AnalyticalRunIssue.Error,
#pragma warning disable CA2208
            _ => throw new ArgumentOutOfRangeException(nameof(qualityControlJudgement))
#pragma warning restore CA2208
        };

        var analyticalRun = qcTestResult.AnalyticalRun;
        if (analyticalRun == null)
        {
            return;
        }

        var qcIssueForAnalyticalRun = await CalculateAnalyticalRunQcIssue(analyticalRun.Id, qcTestResult, qcIssue);

        if (qcIssueForAnalyticalRun != null)
        {
            analyticalRun.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(analyticalRun, CurveIssue: null, CurveControlIssue: null, QualityControlIssue: qcIssueForAnalyticalRun, TestIssue: null));
        }
    }

    private async Task<AnalyticalRunIssue?> CalculateAnalyticalRunQcIssue(int analyticalRunId, QualityControlTestResult qcTestResult, AnalyticalRunIssue qcIssue)
    {
        // We have a new issue 
        if (qcIssue is AnalyticalRunIssue.Warning or AnalyticalRunIssue.Error)
        {
            return qcIssue;
        }

        // So, no QC issues for current qc type!

        // New QC without issues doesn't reset the QC issue for this analytical run (since other QC types latest judgement was not OK)
        if (await AnalyticalRunHasOtherQcIssues(analyticalRunId, qcTestResult.MethodName, qcTestResult.QualityControlName, qcTestResult.TestName))
        {
            return null;
        }

        return qcIssue;
    }

    private async Task<bool> AnalyticalRunHasOtherQcIssues(int analyticalRunId, string methodName, string qualityControlName, string testName)
    {
        var qcList = await
            (from qc in DbContext.QualityControlTestResults
             where qc.AnalyticalRunId == analyticalRunId &&
                   qc.MethodName == methodName &&
                   (qc.QualityControlName != qualityControlName ||
                    qc.TestName != testName)
             select new
             {
                 qc.QualityControlName,
                 qc.TestName,
                 qc.MeasurementDateTime,
                 qc.Judgment
             }).ToListAsync();

        var qcGroupList =
            (from qc in qcList
             group qc by new
             {
                 qc.QualityControlName,
                 qc.TestName
             }
            into qcGroup
             select qcGroup).ToList();

        foreach (var qcGroup in qcGroupList)
        {
            var latestJudgement = qcGroup.OrderByDescending(qc => qc.MeasurementDateTime).Select(qc => qc.Judgment).First();
            if (latestJudgement != QualityControlJudgement.Ok)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<(double? LowerLimit, double? UpperLimit, bool ExcludeLowerLimit, bool ExcludeUpperLimit, double? ReplicateCvLimit)>
        GetLimits(QualityControlTestResult qcTestResult)
    {
        double? lowerLimit = null;
        double? upperLimit = null;
        var excludeLowerLimit = true;
        var excludeUpperLimit = true;
        var replicateCvLimit = await GetReplicateCvLimit(qcTestResult);

        if (qcTestResult is { ExpectedConcentration: not null, ExpectedStdDev: not null, StdDevLimit: not null })
        {
            lowerLimit = qcTestResult.ExpectedConcentration.Value - (qcTestResult.ExpectedStdDev.Value * qcTestResult.StdDevLimit.Value);
            upperLimit = qcTestResult.ExpectedConcentration.Value + (qcTestResult.ExpectedStdDev.Value * qcTestResult.StdDevLimit.Value);

        }
        else if (qcTestResult.MinValue.HasValue || qcTestResult.MaxValue.HasValue)
        {
            if (qcTestResult.MinValue.HasValue)
            {
                lowerLimit = qcTestResult.MinValue.Value;
                excludeLowerLimit = qcTestResult.ExcludeMinValue;
            }
            if (qcTestResult.MaxValue.HasValue)
            {
                upperLimit = qcTestResult.MaxValue.Value;
                excludeUpperLimit = qcTestResult.ExcludeMaxValue;
            }
        }

        return (lowerLimit, upperLimit, excludeLowerLimit, excludeUpperLimit, replicateCvLimit);
    }

    private async Task<double?> GetReplicateCvLimit(QualityControlTestResult qcTestResult)
    {
        return await methodsService.GetQualityControl(qcTestResult.MethodName, qcTestResult.TestName, qcTestResult.QualityControlName, qc => qc.ReplicateCvLimit);
    }

    private static void JudgeAccordingToLimits(double? judgementValue, double percentCv, QualityControlTestResult qcTestResult, double? lowerLimit, double? upperLimit, double? replicateCvLimit, bool excludeLowerLimit, bool excludeUpperLimit)
    {
        if (judgementValue.HasValue)
        {
            // Ok (if not any limits or %CV says something different...)
            qcTestResult.Judgment = QualityControlJudgement.Ok;

            if (replicateCvLimit.HasValue && percentCv > replicateCvLimit.Value)
            {
                qcTestResult.Judgment = QualityControlJudgement.HighCv;
            }

            if (lowerLimit.HasValue)
            {
                if (excludeLowerLimit && judgementValue.Value <= lowerLimit.Value)
                {
                    qcTestResult.Judgment = QualityControlJudgement.Low;
                }
                if (!excludeLowerLimit && judgementValue.Value < lowerLimit.Value)
                {
                    qcTestResult.Judgment = QualityControlJudgement.Low;
                }
            }

            if (upperLimit.HasValue)
            {
                if (excludeUpperLimit && judgementValue.Value >= upperLimit.Value)
                {
                    qcTestResult.Judgment = QualityControlJudgement.High;
                }
                if (!excludeUpperLimit && judgementValue.Value > upperLimit.Value)
                {
                    qcTestResult.Judgment = QualityControlJudgement.High;
                }
            }
        }
    }

    private static double CalculateConcentrationPercentCv(IList<TestReplicate>? replicates)
    {
        var result = 0.0;

        if (replicates == null)
        {
            return result;
        }

        var sd = replicates.Where(r => r is { Concentration: not null, Active: true }).Select(r => r.Concentration!.Value).ToList();

        if (sd is { Count: > 0 })
        {
            result = PercentCv(sd);
        }

        return result;
    }

    private static double CalculateQuotientPercentCv(IList<QualityControlTestReplicate>? replicates)
    {
        var result = 0.0;

        var sd = replicates?.Where(r => r is { Quotient: not null, Active: true }).Select(r => r.Quotient!.Value).ToList();

        if (sd is { Count: > 0 })
        {
            result = PercentCv(sd);
        }

        return result;
    }

    private static double PercentCv(List<double> values)
    {
        double percentCv = 0;
        var count = values.Count;
        if (count > 1)
        {
            var avg = values.Average();

            if (avg > 0)
            {
                var sum = values.Sum(d => (d - avg) * (d - avg));
                var std = Math.Sqrt(sum / count);

                percentCv = std / avg * 100;
            }
        }

        return percentCv;
    }

    private async Task<int> GetLisChannelIdByMethod(string methodName)
    {
        var channelId = await methodsService.GetMethod(methodName, m => m.DefaultLisChannelId);
        return channelId ?? 0;
    }

    private bool LisChannelNeedsQcResults(int channelId)
    {
        var channels = lisSettings.Value.LisChannels;
        if (channelId < 0 || channelId >= channels.Count)
        {
            return false;
        }

        var channel = channels[channelId];
        return channel is
        {
            IsEnabled: true,
            ExportSettings.QualityControl:
            {
                ExportQualityControls: true,
                AutomaticallyWhenQcMeasured: true
            }
        };
    }
}