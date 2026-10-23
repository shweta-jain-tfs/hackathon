using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Request.Application.Commands.ProcessLisExport;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Mapping;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Commands;

internal record SendSampleTestResultsToLisCommand(
    IReadOnlyList<int> TestOrderIds,
    int? LisChannelId = null,
    bool IsManualExport = false) : IRequest;

internal class SendTestResultsToLisCommandHandler(
    IRequestDbContext dbContext,
    IMethodsService methodsService,
    ISender sender,
    IOptionsSnapshot<TubeTypesSettings> tubeSettings,
    IOptionsSnapshot<LisSettings> lisSettings
   ) : IRequestHandler<SendSampleTestResultsToLisCommand>
{
    private Dictionary<string, string?>? _lisMethodNames;
    private Dictionary<(string MethodName, string TestName), MethodTestConfig>? _methodTestInfo;
    private SendSampleTestResultsToLisCommand? _command;

    public async Task Handle(SendSampleTestResultsToLisCommand command, CancellationToken cancellationToken)
    {
        _command = command;

        // Get IDs of all requests that have test orders that must be reported.
        var testOrders = await dbContext.TestOrders
            .Where(t => command.TestOrderIds.Contains(t.Id))
            .Include(t => t.Request)
            .Include(r => r.Request.Patient)
            .Include(r => r.Request.Requestor)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (testOrders.Count == 0)
        {
            return;
        }

        // Get panel information for test orders that have panels assigned
        var panelLookup = await CreatePanelLookupAsync(testOrders, cancellationToken);

        // get lab method names
        var pharosMethodNames = testOrders.Select(x => x.MethodName).ToList();
        var pharosTestNames = testOrders.Select(x => x.TestName).ToList();
        var methods = await methodsService.GetMethods(
            x => pharosMethodNames.Contains(x.MethodName),
            x => new
            {
                MethodName = x.MethodName,
                MethodNameLis = x.MethodNameLis,
                DefaultLisChannelId = x.DefaultLisChannelId,
                Tests = x.Tests
                    .Where(test => pharosTestNames.Contains(test.TestName))
                    .Select(test => new
                    {
                        test.TestName,
                        test.FullTestName,
                        test.ReportConcentration,
                        test.ReportClass,
                        test.ReportCutOff,
                        test.ReportQuotient
                    })
                    .ToList()
            });

        _lisMethodNames = methods
            .ToDictionary(x => x.MethodName, x => x.MethodNameLis);

        var methodDefaultChannels = methods
            .ToDictionary(x => x.MethodName, x => x.DefaultLisChannelId);

        _methodTestInfo = methods
            .SelectMany(method => method.Tests.Select(test => new
            {
                method.MethodName,
                test.TestName,
                MethodTestConfig= new MethodTestConfig(
                    test.FullTestName,
                    test.ReportConcentration,
                    test.ReportClass,
                    test.ReportCutOff,
                    test.ReportQuotient != QuotientReportMode.NoReport)
            }))
            .ToDictionary(x => (x.MethodName, x.TestName), x => x.MethodTestConfig);

        // Manual exports can contain test orders from multiple LIS channels.
        var groups = testOrders
            .GroupBy(
                t => GetEffectiveLisChannelIdOrDefault(t, methodDefaultChannels),
                (key, g) => new { LisChannelId = key, TestOrders = g.ToList() })
            .ToList();

        foreach (var group in groups)
        {
            await SendTestResultsToLisChannel(group.LisChannelId, group.TestOrders, panelLookup, cancellationToken);
        }
    }

    private async Task SendTestResultsToLisChannel(int? lisChannelId, List<TestOrder> testOrders, Dictionary<int, string> panelLookup, CancellationToken cancellationToken)
    {
        // get test results for the test orders
        var testResults = await sender.Send(new GetSampleTestResultsForLisQuery(testOrders.Select(x => x.Uid).ToList()), cancellationToken);

        // Extract test order GUIDs for tracking
        var testOrderGuids = testOrders.Select(x => x.Uid).ToList();

        // Use the IsManualExport flag from command, or false if not specified
        var isManualExport = _command?.IsManualExport ?? false;

        var lisPatients = new List<LisPatient>();

        var lisPatient = new LisPatient();

        // process test orders without patients
        var requests = testOrders
            .Where(x => x.Request.Patient == null)
            .Select(x => x.Request)
            .Distinct()
            .ToList();

        await MapPatientRequestsToSamples(lisPatient, requests, testResults, panelLookup, lisChannelId, cancellationToken);

        if (lisPatient.Samples.Count > 0)
        {
            lisPatients.Add(lisPatient);
        }

        // process test orders with patients
        var patients = testOrders
            .Where(x => x.Request.Patient != null)
            .Select(x => x.Request.Patient)
            .Distinct()
            .ToList();
        foreach (var patient in patients)
        {
            lisPatient = LisMapper.ToLisPatient(patient!);

            await MapPatientRequestsToSamples(lisPatient, patient!.Requests!, testResults, panelLookup, lisChannelId, cancellationToken);
            if (lisPatient.Samples.Count > 0)
            {
                lisPatients.Add(lisPatient);
            }
        }

        // send data to LIS
        if (lisPatients.Count > 0)
        {
            await sender.Send(new Server.Application.Lis.SendSampleTestResultsToLisCommand(
                lisChannelId,
                lisPatients,
                testOrderGuids,
                isManualExport), cancellationToken);
        }
    }

    private async Task MapPatientRequestsToSamples(LisPatient patient, IList<Domain.Request> requests, List<LisSampleTestResult> sampleTestResults, Dictionary<int, string> panelLookup, int? lisChannelId, CancellationToken cancellationToken)
    {
        foreach (var request in requests)
        {
            if (request.Patient != null)
            {
                patient.PatientAge = request.PatientAge;
            }

            var sample = LisMapper.ToLisSample(request);

            sample.TubeType = MapTubeDefinitionIdToTubeType(request.SampleTubeType);
            var sortByTestName = lisSettings.ShouldExportTestsInAlphabeticalOrder(lisChannelId ?? 0);

            var orderedTestOrders = sortByTestName
                                    ? request.TestOrders.OrderBy(to => to.TestName).ToList()
                                    : request.TestOrders;
            var tubeIds = request.TestOrders.Where(t => !string.IsNullOrEmpty(t.TubeId)).Select(t => t.TubeId!).Distinct().ToList();
            var tubePositions = await sender.Send(new GetSampleTubePositionsForLisQuery(tubeIds), cancellationToken);

            foreach (var test in orderedTestOrders)
            {
                var lisTest = LisMapper.ToLisTestOrder(test);
                lisTest.PreDilutionFactor = request.PreDilutionFactor;

                // Set LIS Test Settings Info when a matching method and test are found.
                if (_methodTestInfo!.TryGetValue((test.MethodName, test.TestName), out var testConfigInfo))
                {
                    lisTest.FullTestName = testConfigInfo.FullTestName;
                    lisTest.ReportConcentration = testConfigInfo.ReportConcentration;
                    lisTest.ReportClass = testConfigInfo.ReportClass;
                    lisTest.ReportCutOff = testConfigInfo.ReportCutOff;
                    lisTest.ReportQuotient = testConfigInfo.ReportQuotient;
                }

                // we need lis method name here, not out internal method name
                if (_lisMethodNames!.TryGetValue(test.MethodName, out var lisName) && lisName != null)
                {
                    lisTest.MethodName = lisName;
                }
                else
                {
                    lisTest.MethodName = test.MethodName;
                }

                // Populate panel name if this test order assigned to panel
                foreach (var panelId in test.TestPanelIds)
                {
                    if (panelLookup.TryGetValue(panelId, out var panelName))
                    {
                        lisTest.PanelNames.Add(panelName);
                    }
                }

                var testResult = sampleTestResults.FirstOrDefault(x => x.TestOrderId == test.Uid);

                if (testResult == null)
                {
                    // TODO: we probably, need a better handling for cases when there is no result yet
                    continue;
                }

                lisTest.TestResult = testResult;
                var tubePosition = tubePositions.FirstOrDefault(t => t.TubeId == test.TubeId);
                lisTest.TrayId = tubePosition?.TrayId;
                lisTest.TubePosition = tubePosition?.TubePosition;
                sample.TestOrders.Add(lisTest);
            }

            if (sample.TestOrders.Count > 0)
            {
                patient.Samples.Add(sample);
            }
        }
    }

    private string MapTubeDefinitionIdToTubeType(int? tubeDefinitionId)
    {
        const string NormalTubeType = "N";
        const string PediatricTubeType = "C";

        if (tubeDefinitionId is null)
        {
            return NormalTubeType;
        }

        var settings = tubeSettings.Value;

        if (int.TryParse(settings.NormalTubeDefinitionId, out var normalId) && normalId == tubeDefinitionId)
        {
            return NormalTubeType;
        }
        else if (int.TryParse(settings.PediatricTubeDefinitionId, out var pediatricId) && pediatricId == tubeDefinitionId)
        {
            return PediatricTubeType;
        }

        return tubeDefinitionId.Value.ToString();
    }

    /// <summary>
    /// Creates a lookup dictionary mapping panel IDs to panel names for test orders that have panels assigned.
    /// </summary>
    /// <param name="testOrders">The list of test orders to extract panel information from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary mapping panel ID to panel name.</returns>
    private async Task<Dictionary<int, string>> CreatePanelLookupAsync(List<TestOrder> testOrders, CancellationToken cancellationToken)
    {
        var panelLookup = new Dictionary<int, string>();

        // Extract unique panel IDs from all test orders
        var panelIds = testOrders
            .Where(t => t.TestPanelIds.Count > 0)
            .SelectMany(t => t.TestPanelIds)
            .Distinct()
            .ToList();

        if (panelIds.Count == 0)
        {
            return panelLookup;
        }

        // Fetch panel information and populate lookup
        foreach (var panelId in panelIds)
        {
            var panel = await sender.Send(new GetPanelByIdQuery(panelId), cancellationToken).ConfigureAwait(false);
            if (panel != null)
            {
                panelLookup[panelId] = panel.PanelName;
            }
        }

        return panelLookup;
    }

    private static int GetEffectiveLisChannelIdOrDefault(
        TestOrder testOrder,
        IReadOnlyDictionary<string, int?> methodDefaultChannels)
    {
        return testOrder.LisChannelId
               ?? methodDefaultChannels.GetValueOrDefault(testOrder.MethodName)
               ?? 0;
    }

    private sealed record MethodTestConfig(
        string FullTestName,
        bool ReportConcentration,
        bool ReportClass,
        bool ReportCutOff,
        bool ReportQuotient);
}
