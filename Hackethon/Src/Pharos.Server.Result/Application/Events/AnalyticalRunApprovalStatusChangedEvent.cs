using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Mapping;

namespace Pharos.Server.Result.Application.Events;

internal record AnalyticalRunApprovalStatusChangedEvent(int AnalyticalRunId, ResultApprovalStatus NewApprovalStatus) : INotification;

internal sealed class AnalyticalRunApprovalStatusChangedEventHandler(
    IResultDbContext dbContext,
    IOptionsSnapshot<LisSettings> lisSettings,
    ISender sender,
    LisTestResultMapper mapper,
    IMethodsService methodsService) : INotificationHandler<AnalyticalRunApprovalStatusChangedEvent>
{
    public async Task Handle(AnalyticalRunApprovalStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.NewApprovalStatus == ResultApprovalStatus.NotApproved || !lisSettings.Value.LisChannels.Any(LisChannelNeedsQcResults))
        {
            return;
        }

        var qualityControlTestResults = await dbContext.QualityControlTestResults
            .Where(tr => tr.AnalyticalRunId == notification.AnalyticalRunId && tr.Status != TestResultStatus.Reported)
            .Include(tr => tr.Replicates!)
            .ThenInclude(r => r.TestReplicateReagents)
            .ToListAsync(cancellationToken);

        if (qualityControlTestResults.Count == 0)
        {
            return;
        }

        var methodNames = qualityControlTestResults.Select(result => result.MethodName).Distinct().ToList();
        var lisChannelIdsByMethod = await GetLisChannelIdsByMethod(methodNames);

        var qualityControlResultsForLis = await mapper.ToLisTestResults(qualityControlTestResults);
        foreach (var qualityControlResult in qualityControlResultsForLis)
        {
            var methodName = qualityControlResult.MethodName;
            if (!lisChannelIdsByMethod.TryGetValue(methodName, out var lisChannelId) || !LisChannelNeedsQcResults(lisChannelId))
            {
                continue;
            }

            await sender.Send(new SendQualityControlTestResultToLisCommand(lisChannelId, qualityControlResult), cancellationToken);
        }
    }

    private async Task<Dictionary<string, int>> GetLisChannelIdsByMethod(List<string> methodNames)
    {
        var methods = await methodsService.GetMethods(
            m => methodNames.Contains(m.MethodName),
            m => new { m.MethodName, m.DefaultLisChannelId });

        return methods.ToDictionary(m => m.MethodName, m => m.DefaultLisChannelId ?? 0);
    }

    private bool LisChannelNeedsQcResults(int channelId)
    {
        var channels = lisSettings.Value.LisChannels;
        if (channelId < 0 || channelId >= channels.Count)
        {
            return false;
        }

        return LisChannelNeedsQcResults(channels[channelId]);
    }

    private static bool LisChannelNeedsQcResults(LisChannel channel)
    {
        return channel is
        {
            IsEnabled: true,
            ExportSettings.QualityControl:
            {
                ExportQualityControls: true,
                AutomaticallyWhenRunApprovedOrRejected: true
            }
        };
    }
}

