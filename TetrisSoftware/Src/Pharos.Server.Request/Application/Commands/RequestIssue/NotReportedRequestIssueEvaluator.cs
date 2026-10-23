using Microsoft.Extensions.Options;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Request.Application.Commands.ProcessLisExport;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Commands.RequestIssue;

internal sealed class NotReportedRequestIssueEvaluator(
    IOptionsSnapshot<LisSettings> lisSettings,
    IMethodsService methodsService)
{
    /// <summary>
    /// Checks if a completed request needs a "Report results" task.
    /// </summary>
    public async Task<bool> ShouldCreateAsync(Domain.Request request)
    {
        if (request.RequestState != RequestState.Completed)
        {
            return false;
        }

        var reportableTestOrders = request.TestOrders
            .Where(IsWaitingToBeReported)
            .ToList();

        if (reportableTestOrders.Count == 0)
        {
            return false;
        }

        var defaultLisChannelIdsByMethodName = await GetDefaultLisChannelIdsByMethodNameAsync(reportableTestOrders);
        return ShouldCreate(request, defaultLisChannelIdsByMethodName);
    }

    /// <summary>
    /// Checks if a request needs a "Report results" task using already loaded method settings.
    /// </summary>
    public bool ShouldCreate(
        Domain.Request request,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName)
    {
        if (request.RequestState != RequestState.Completed)
        {
            return false;
        }

        return request.TestOrders
            .Where(IsWaitingToBeReported)
            .Any(testOrder => HasAutomaticExportEnabled(testOrder, defaultLisChannelIdsByMethodName));
    }

    private bool HasAutomaticExportEnabled(
        TestOrder testOrder,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName)
    {
        var lisChannelId = GetEffectiveLisChannelId(testOrder, defaultLisChannelIdsByMethodName);
        return lisSettings.GetAutomaticSampleExportMode(lisChannelId) != LisAutoExportTrigger.NotUsed;
    }

    /// <summary>
    /// Loads the default LIS channel for test orders that do not have one.
    /// </summary>
    public async Task<Dictionary<string, int?>> GetDefaultLisChannelIdsByMethodNameAsync(IEnumerable<TestOrder> testOrders)
    {
        // A test order can override the LIS channel; otherwise use the method default.
        var methodNames = testOrders
            .Where(testOrder => testOrder.LisChannelId == null)
            .Select(testOrder => testOrder.MethodName)
            .Distinct()
            .ToList();

        if (methodNames.Count == 0)
        {
            return [];
        }

        var methods = await methodsService.GetMethods(
            method => methodNames.Contains(method.MethodName),
            method => new MethodDefaultLisChannel(method.MethodName, method.DefaultLisChannelId));

        return methods.ToDictionary(method => method.MethodName, method => method.DefaultLisChannelId);
    }

    private static bool IsWaitingToBeReported(TestOrder testOrder)
    {
        return testOrder.State is TestOrderState.Approved or TestOrderState.Rejected;
    }

    private static int GetEffectiveLisChannelId(
        TestOrder testOrder,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName)
    {
        return testOrder.LisChannelId
               ?? defaultLisChannelIdsByMethodName.GetValueOrDefault(testOrder.MethodName)
               ?? 0;
    }
}

internal sealed record MethodDefaultLisChannel(string MethodName, int? DefaultLisChannelId);
