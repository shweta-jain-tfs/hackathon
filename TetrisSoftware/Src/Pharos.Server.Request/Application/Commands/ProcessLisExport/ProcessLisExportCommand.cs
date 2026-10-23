using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Request.Domain;
using DomainRequest = Pharos.Server.Request.Domain.Request;
using DomainTestOrder = Pharos.Server.Request.Domain.TestOrder;
using TestOrderState = Pharos.Server.Domain.Enums.TestOrderState;

namespace Pharos.Server.Request.Application.Commands.ProcessLisExport;

/// <summary>
/// Command to process automatic LIS exports with channel-specific filtering and expansion.
/// Applies the three-stage pipeline:
/// 1. Determine export (AutomaticSampleExport setting)
/// 2. Expand test selection (IncludeOtherTestsInAutomaticExports setting)
/// 3. Filter by status (WhatToExport setting)
/// </summary>
/// <param name="Requests">Requests with test orders for export processing.</param>
/// <param name="TestOrderUids">Test order Uids that became approved/rejected and triggered this export.</param>
/// <param name="IsManualExport">Whether the export was manually requested from the client.</param>
internal record ProcessLisExportCommand(
    List<DomainRequest> Requests,
    List<Guid> TestOrderUids,
    bool IsManualExport = false) : IRequest;

/// <summary>
/// Handles automatic LIS export processing with channel-specific filtering and expansion.
/// Determine export trigger (AutomaticSampleExport setting)
/// Expand test selection (IncludeOtherTestsInAutomaticExports setting)
/// Filter by status (WhatToExport setting)
/// </summary>
internal sealed class ProcessLisExportCommandHandler(
    IOptionsSnapshot<LisSettings> lisSettings,
    IMethodsService methodsService,
    ISender sender) : IRequestHandler<ProcessLisExportCommand>
{
    public async Task Handle(ProcessLisExportCommand request, CancellationToken cancellationToken)
    {
        if (request.TestOrderUids.Count == 0 || request.Requests.Count == 0)
        {
            return;
        }

        // Extract all test orders and group triggered tests by channel
        var allTestOrders = request.Requests
            .SelectMany(r => r.TestOrders)
            .ToList();

        var defaultLisChannelIdsByMethodName = await GetDefaultLisChannelIdsByMethodNameAsync(allTestOrders);

        var triggeredTests = allTestOrders
            .Where(to => request.TestOrderUids.Contains(to.Uid))
            .ToList();

        var triggeredTestsByChannel = triggeredTests
            .GroupBy(to => GetEffectiveLisChannelIdOrDefault(to, defaultLisChannelIdsByMethodName))
            .ToDictionary(g => g.Key, g => g.ToList());

        if (request.IsManualExport)
        {
            foreach (var (channelId, channelTriggeredTests) in triggeredTestsByChannel)
            {
                await ProcessManualChannelExportsAsync(
                    channelId,
                    channelTriggeredTests,
                    request.Requests,
                    defaultLisChannelIdsByMethodName,
                    cancellationToken);
            }

            return;
        }

        var sampleLevelAffectedRequests = new Dictionary<int, DomainRequest>();

        foreach (var (channelId, channelTriggeredTests) in triggeredTestsByChannel)
        {
            var exportMode = lisSettings.GetAutomaticSampleExportMode(channelId);

            if (exportMode == LisAutoExportTrigger.WhenTestIsApprovedOrRejected)
            {
                await ProcessTestLevelChannelExportsAsync(
                    channelId,
                    channelTriggeredTests,
                    request.Requests,
                    defaultLisChannelIdsByMethodName,
                    cancellationToken);
            }
            else if (exportMode == LisAutoExportTrigger.WhenSampleIsApprovedOrRejected)
            {
                foreach (var affectedRequest in GetRequestsForTestOrders(channelTriggeredTests, request.Requests))
                {
                    sampleLevelAffectedRequests.TryAdd(affectedRequest.Id, affectedRequest);
                }
            }
        }

        var sampleLevelRequests = sampleLevelAffectedRequests.Values.ToList();
        var sampleLevelChannelIds = sampleLevelRequests
            .SelectMany(r => r.TestOrders)
            .Select(to => GetEffectiveLisChannelIdOrDefault(to, defaultLisChannelIdsByMethodName))
            .Distinct()
            .ToList();

        foreach (var channelId in sampleLevelChannelIds)
        {
            await ProcessSampleLevelChannelExportsAsync(
                channelId,
                sampleLevelRequests,
                defaultLisChannelIdsByMethodName,
                cancellationToken);
        }
    }

    private async Task<Dictionary<string, int?>> GetDefaultLisChannelIdsByMethodNameAsync(List<DomainTestOrder> testOrders)
    {
        var methodNames = testOrders
            .Select(to => to.MethodName)
            .Distinct()
            .ToList();

        var methods = await methodsService.GetMethods(
            x => methodNames.Contains(x.MethodName),
            x => new
            {
                x.MethodName,
                x.DefaultLisChannelId
            });

        return methods.ToDictionary(x => x.MethodName, x => x.DefaultLisChannelId);
    }

    /// <summary>
    /// Processes manually selected request exports for a specific channel.
    /// </summary>
    private async Task ProcessManualChannelExportsAsync(
        int channelId,
        List<DomainTestOrder> selectedTests,
        List<DomainRequest> allRequests,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName,
        CancellationToken cancellationToken)
    {
        var filteredTests = FilterLocallyEnteredSamples(selectedTests, allRequests, channelId);
        var testsToExport = FilterByWhatToExportSetting(filteredTests, channelId);

        await SendExportTestsForChannel(
            channelId,
            testsToExport,
            allRequests,
            defaultLisChannelIdsByMethodName,
            cancellationToken,
            isManualExport: true);
    }

    /// <summary>
    /// Processes exports for a specific channel with its own settings.
    /// </summary>
    private async Task ProcessTestLevelChannelExportsAsync(
        int channelId,
        List<DomainTestOrder> triggeredTests,
        List<DomainRequest> allRequests,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName,
        CancellationToken cancellationToken)
    {
        var exportMode = lisSettings.GetAutomaticSampleExportMode(channelId);

        if (exportMode != LisAutoExportTrigger.WhenTestIsApprovedOrRejected)
        {
            return;
        }

        // Filter out locally entered samples if the setting is not enabled
        var filteredTriggeredTests = FilterLocallyEnteredSamples(triggeredTests, allRequests, channelId);

        var testsToExport = PrepareTestsForExport(filteredTriggeredTests, allRequests, channelId, defaultLisChannelIdsByMethodName);

        await SendExportTestsForChannel(channelId, testsToExport, allRequests, defaultLisChannelIdsByMethodName, cancellationToken);
    }

    /// <summary>
    /// Processes completed-sample exports for a specific channel with its own settings.
    /// </summary>
    private async Task ProcessSampleLevelChannelExportsAsync(
        int channelId,
        List<DomainRequest> affectedRequests,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName,
        CancellationToken cancellationToken)
    {
        var exportMode = lisSettings.GetAutomaticSampleExportMode(channelId);

        if (exportMode != LisAutoExportTrigger.WhenSampleIsApprovedOrRejected)
        {
            return;
        }

        var testsToExport = PrepareCompletedSamplesForExport(affectedRequests, channelId, defaultLisChannelIdsByMethodName);

        await SendExportTestsForChannel(channelId, testsToExport, affectedRequests, defaultLisChannelIdsByMethodName, cancellationToken);
    }

    private async Task SendExportTestsForChannel(
        int channelId,
        List<DomainTestOrder> exportTests,
        List<DomainRequest> allRequests,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName,
        CancellationToken cancellationToken,
        bool isManualExport = false)
    {
        var testsToExport = ApplyRetestFilters(exportTests, allRequests, channelId, defaultLisChannelIdsByMethodName);

        if (testsToExport.Count == 0)
        {
            return;
        }

        var testOrderIds = testsToExport.Select(to => to.Id).ToList();
        await sender.Send(
            new SendSampleTestResultsToLisCommand(
                TestOrderIds: testOrderIds,
                LisChannelId: channelId,
                IsManualExport: isManualExport),
            cancellationToken);
    }

    /// <summary>
    /// Applies retest inclusion/exclusion rules to the export list.
    ///   IncludeNewTestsForRetestedSamples
    ///   IncludeRejectedTestsForRetestedSamples
    /// </summary>
    private List<DomainTestOrder> ApplyRetestFilters(
        List<DomainTestOrder> testsToExport,
        List<DomainRequest> allRequests,
        int channelId,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName)
    {
        var includeNewTests = lisSettings.IncludeNewTestsForRetestedSamples(channelId);
        var includeRejectedResults = lisSettings.IncludeRejectedTestsForRetestedSamples(channelId);

        var testsToExportDict = new Dictionary<Guid, DomainTestOrder>(testsToExport
            .DistinctBy(to => to.Uid)
            .ToDictionary(to => to.Uid));

        foreach (var test in testsToExport)
        {
            DomainTestOrder? rejectedRetest = null;
            DomainTestOrder? newRetest = null;

            if (test.State == TestOrderState.Rejected)
            {
                newRetest = GetNewRetestIfExists(test, allRequests, channelId, defaultLisChannelIdsByMethodName);
                rejectedRetest = newRetest != null ? test : null;
            }
            else if (IsResultNotReady(test.State))
            {
                rejectedRetest = GetRejectedRetestIfExists(test, allRequests, channelId, defaultLisChannelIdsByMethodName);
                newRetest = rejectedRetest != null ? test : null;
            }

            ApplyRetestSetting(includeRejectedResults, rejectedRetest, testsToExportDict);
            ApplyRetestSetting(includeNewTests, newRetest, testsToExportDict);
        }

        return testsToExportDict.Values.ToList();
    }

    /// <summary>
    /// Applies the inclusion/exclusion setting to the test order in the export dictionary.
    /// </summary>
    private static void ApplyRetestSetting(bool include, DomainTestOrder? testOrder, Dictionary<Guid, DomainTestOrder> testsToExportDict)
    {
        if (testOrder is not null)
        {
            if (include)
            {
                testsToExportDict.TryAdd(testOrder.Uid, testOrder);
            }
            else
            {
                testsToExportDict.Remove(testOrder.Uid);
            }
        }
    }

    /// <summary>
    /// Checks if the passed test order is rejected and has a new retest.
    /// </summary>
    private static DomainTestOrder? GetNewRetestIfExists(
        DomainTestOrder testOrder,
        List<DomainRequest> allRequests,
        int channelId,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName)
    {
        var request = FindRequestForTestOrder(testOrder, allRequests);
        if (request is null)
        {
            return null;
        }

        var newRetest = request.TestOrders
            .FirstOrDefault(to => to.MethodName == testOrder.MethodName
                               && to.TestName == testOrder.TestName
                               && to.Uid != testOrder.Uid
                               && GetEffectiveLisChannelIdOrDefault(to, defaultLisChannelIdsByMethodName) == channelId
                               && IsResultNotReady(to.State));

        return newRetest;
    }

    /// <summary>
    /// Checks if the passed test order is a new retest and has a rejected retest.
    /// </summary>
    private static DomainTestOrder? GetRejectedRetestIfExists(
        DomainTestOrder testOrder,
        List<DomainRequest> allRequests,
        int channelId,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName)
    {
        var request = FindRequestForTestOrder(testOrder, allRequests);
        if (request is null)
        {
            return null;
        }

        var rejectedRetest = request.TestOrders
            .FirstOrDefault(to => to.MethodName == testOrder.MethodName
                               && to.TestName == testOrder.TestName
                               && to.Uid != testOrder.Uid
                               && GetEffectiveLisChannelIdOrDefault(to, defaultLisChannelIdsByMethodName) == channelId
                               && to.State == TestOrderState.Rejected);

        return rejectedRetest;
    }

    private static bool IsResultNotReady(TestOrderState orderState)
    {
        return orderState is TestOrderState.NotStarted or
                             TestOrderState.Scheduled or
                             TestOrderState.UnableToStart or
                             TestOrderState.Started or
                             TestOrderState.Pipetted;
    }

    /// <summary>
    /// Prepares tests for export with expansion and filtering.
    /// Expansion (IncludeOtherTestsInAutomaticExports)
    /// Filtering (WhatToExport)
    /// </summary>
    private List<DomainTestOrder> PrepareTestsForExport(
        List<DomainTestOrder> triggeredTests,
        List<DomainRequest> allRequests,
        int channelId,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName)
    {
        var expandedTests = ExpandTestsBasedOnSetting(triggeredTests, allRequests, channelId, defaultLisChannelIdsByMethodName);

        var filteredTests = FilterByWhatToExportSetting(expandedTests, channelId);
        return filteredTests;
    }

    /// <summary>
    /// Prepares complete samples for export with filtering.
    /// A sample is complete when ALL test orders are in Approved, Rejected, or Reported state.
    /// </summary>
    private List<DomainTestOrder> PrepareCompletedSamplesForExport(
        List<DomainRequest> affectedRequests,
        int channelId,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName)
    {
        var candidateRequests = lisSettings.IncludeLocallyEnteredSamples(channelId)
            ? affectedRequests
            : affectedRequests.Where(r => r.Source == RequestSource.Lis).ToList();

        // Filter to only completed samples (all tests in final states)
        var completedSamples = candidateRequests
            .Where(r => r.TestOrders.All(to =>
                to.State is TestOrderState.Approved or
                          TestOrderState.Rejected or
                          TestOrderState.Reported))
            .ToList();

        if (completedSamples.Count == 0)
        {
            return new List<DomainTestOrder>();
        }

        // Get all test orders from completed samples for this channel
        var testOrders = completedSamples
            .SelectMany(r => r.TestOrders)
            .Where(to => GetEffectiveLisChannelIdOrDefault(to, defaultLisChannelIdsByMethodName) == channelId)
            .ToList();

        testOrders = ApplyIncludeOtherTestsForCompletedSample(testOrders, channelId);

        var filteredTests = FilterByWhatToExportSetting(testOrders, channelId);

        return filteredTests;
    }

    private List<DomainTestOrder> ApplyIncludeOtherTestsForCompletedSample(
        List<DomainTestOrder> testOrders,
        int channelId)
    {
        var includeOtherTests = lisSettings.GetIncludeOtherTests(channelId);

        if (includeOtherTests == IncludeOtherTestsInAutomaticExports.AllTestsForSampleExceptAlreadyReported)
        {
            return testOrders
                .Where(to => to.State != TestOrderState.Reported)
                .ToList();
        }

        return testOrders;
    }

    /// <summary>
    /// Expands test selection based on IncludeOtherTestsInAutomaticExports setting.
    /// </summary>
    private List<DomainTestOrder> ExpandTestsBasedOnSetting(
        List<DomainTestOrder> triggeredTests,
        List<DomainRequest> allRequests,
        int channelId,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName)
    {
        var includeOtherTests = lisSettings.GetIncludeOtherTests(channelId);

        if (includeOtherTests == IncludeOtherTestsInAutomaticExports.NotUsed)
        {
            return triggeredTests;
        }

        // Get request IDs that contain triggered tests
        var triggeredRequestIds = triggeredTests
            .Select(to => FindRequestForTestOrder(to, allRequests)?.Id)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToHashSet();

        // Expand to all tests from those requests for this channel
        var expandedTests = allRequests
            .Where(r => triggeredRequestIds.Contains(r.Id))
            .SelectMany(r => r.TestOrders)
            .Where(to => GetEffectiveLisChannelIdOrDefault(to, defaultLisChannelIdsByMethodName) == channelId)
            .ToList();

        // Apply exclusion for "except already Reported"
        if (includeOtherTests == IncludeOtherTestsInAutomaticExports.AllTestsForSampleExceptAlreadyReported)
        {
            expandedTests = expandedTests
                .Where(to => to.State != TestOrderState.Reported)
                .ToList();
        }

        return expandedTests;
    }

    /// <summary>
    /// Filters tests based on WhatToExport setting.
    /// </summary>
    private List<DomainTestOrder> FilterByWhatToExportSetting(List<DomainTestOrder> testOrders, int channelId)
    {
        var whatToExport = lisSettings.GetWhatToExport(channelId);

        return whatToExport switch
        {
            WhatToExport.AllStatuses => testOrders,

            WhatToExport.OnlyApprovedResults => testOrders
                .Where(to => to.State == TestOrderState.Approved)
                .ToList(),

            WhatToExport.OnlyApprovedAndRejectedResults => testOrders
                .Where(to => to.State is TestOrderState.Approved or
                                        TestOrderState.Rejected)
                    .ToList(),

            _ => testOrders
        };
    }

    /// <summary>
    /// Filters out test orders from locally entered samples if the setting is not enabled.
    /// </summary>
    private List<DomainTestOrder> FilterLocallyEnteredSamples(
        List<DomainTestOrder> testOrders,
        List<DomainRequest> allRequests,
        int channelId)
    {
        if (lisSettings.IncludeLocallyEnteredSamples(channelId))
        {
            return testOrders;
        }

        // Filter to only include test orders from externally sourced requests.
        // Import requests are considered external input and should be exported even
        // when locally entered samples are excluded.
        var externallySourcedRequestIds = allRequests
            .Where(r => r.Source is RequestSource.Lis or RequestSource.Import)
            .Select(r => r.Id)
            .ToHashSet();

        var filteredTests = testOrders
            .Where(to =>
            {
                var request = FindRequestForTestOrder(to, allRequests);
                return request != null && externallySourcedRequestIds.Contains(request.Id);
            })
            .ToList();

        return filteredTests;
    }

    /// <summary>
    /// Helper to find which request contains a test order.
    /// </summary>
    private static DomainRequest? FindRequestForTestOrder(DomainTestOrder testOrder, List<DomainRequest> requests)
    {
        return requests.FirstOrDefault(r => r.TestOrders.Any(to => to.Uid == testOrder.Uid));
    }

    private static IEnumerable<DomainRequest> GetRequestsForTestOrders(
        List<DomainTestOrder> testOrders,
        List<DomainRequest> requests)
    {
        var testOrderUids = testOrders.Select(to => to.Uid).ToHashSet();
        return requests.Where(r => r.TestOrders.Any(to => testOrderUids.Contains(to.Uid)));
    }

    private static int GetEffectiveLisChannelIdOrDefault(
        DomainTestOrder testOrder,
        IReadOnlyDictionary<string, int?> defaultLisChannelIdsByMethodName)
    {
        return testOrder.LisChannelId
               ?? defaultLisChannelIdsByMethodName.GetValueOrDefault(testOrder.MethodName)
               ?? 0;
    }
}
