using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Parameters;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reflex;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Infrastructure;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Mapping;
using Pharos.Server.Request.Domain;
using EntityState = Pharos.Server.Application.Request.EntityState;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;
using RequestCreatedEvent = Pharos.Server.Request.Application.Events.RequestCreatedEvent;

namespace Pharos.Server.Request.Application.Commands;

internal sealed class ProcessLisTestOrdersCommandHandler(
    IRequestDbContext dbContext,
    IOptionsSnapshot<RequestSettings> requestSettings,
    IOptionsSnapshot<TubeTypesSettings> tubeSettings,
    ILogger<ProcessLisTestOrdersCommandHandler> logger,
    ISender sender,
    IMethodsService methodsService,
    IDateTime dateTime)
    : IRequestHandler<ProcessLisTestOrdersCommand, bool>
{
    private readonly List<Domain.Request> _requestList = new();
    private int _lisChannelId;

    /// <summary>
    /// Handle imported test order message
    /// </summary>
    /// <param name="command">The test order message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<bool> Handle(ProcessLisTestOrdersCommand command, CancellationToken cancellationToken)
    {
        try
        {
            _lisChannelId = command.LisChannelId;

            // Handle patients and their samples
            foreach (var importedPatient in command.Patients)
            {
                Patient? patient = null;

                // Handle patient information if patient setting is enabled.
                if (requestSettings.Value.IsPatientInformationEnabled)
                {
                    // Try to find existing patient by various identifiers
                    if (!string.IsNullOrEmpty(importedPatient.PracticePatientId))
                    {
                        patient = await dbContext.Patients
                            .FirstOrDefaultAsync(p => p.PatientId == importedPatient.PracticePatientId, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    if (patient == null && !string.IsNullOrEmpty(importedPatient.PracticePatientId))
                    {
                        patient = new Patient();
                        dbContext.Patients.Add(patient);
                    }

                    // Map patient data only if we found or created a patient
                    if (patient != null)
                    {
                        LisMapper.UpdatePatientFromLis(importedPatient, patient);
                    }
                }

                foreach (var importedSample in importedPatient.Samples)
                {
                    var request = await HandleImportedSample(importedPatient, patient, importedSample, cancellationToken);

                    if (request != null)
                    {
                        _requestList.Add(request);
                    }
                }
            }

            await RemoveScheduledJob(_requestList, cancellationToken);

            AddDomainEvents();

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            foreach (var request in _requestList)
            {
                await sender.Send(new SyncTestOrdersWithInstrumentCommand(request), cancellationToken).ConfigureAwait(false);
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process LIS test orders.");
            // Todo : send Pharos error to UI
            return false;
        }
    }

    private async Task RemoveScheduledJob(List<Domain.Request> requestList, CancellationToken cancellationToken)
    {
        var sampleIds = requestList.Select(r => r.SampleId).Distinct().ToList();
        await sender.Send(new RemoveMissingTestOrdersJobCommand(sampleIds), cancellationToken);
    }

    /// <summary>
    /// Handle an imported sample
    /// </summary>
    private async Task<Domain.Request?> HandleImportedSample(LisPatient importedPatient, Patient? patient, LisSample importedSample, CancellationToken cancellationToken)
    {
        var sampleTube = GetTubeDefinition(importedSample);
        if (sampleTube == null)
        {
            logger.LogError("Import of sample with tests is skipped. Invalid sample tube type {TubeType}, Sample ID = {SampleId}", importedSample.TubeType, importedSample.SampleId);

            await SendPharosError(PharosErrorCodes.InvalidSampleTubeType, ErrorClassification.Error, importedSample.TubeType ?? "N/P", importedSample.SampleId);
            return null;
        }

        var request = await GetRequest(importedSample, cancellationToken);

        // Store some original values to preserve them during mapping
        var originalRequestor = request.Requestor;
        var originalRequestorId = request.RequestorId;
        var sampleCollectionDate = request.SampleCollectionDate;
        var requestDate = request.RequestDate;
        var started = request.RequestState != RequestState.NotStarted;
        var sampleTubeType = request.SampleTubeType;

        // Only handle requestor if enabled.
        Requestor? requestor = null;
        if (requestSettings.Value.IsRequestorInformationEnabled)
        {
            requestor = await HandleImportedRequestor(importedSample.Requestor, cancellationToken);
        }

        // Map sample data but preserve requestor information when disabled
        if (request.Id == 0)
        {
            request.SampleId = importedSample.SampleId;
        }
        request.RequestId = importedSample.RequestId;
        request.RequestDate = importedSample.RequestDate;
        request.SampleCollectionDate = importedSample.SampleCollectionDate;

        // If requestor information is disabled, restore the original requestor state
        if (!requestSettings.Value.IsRequestorInformationEnabled)
        {
            // Restore original requestor data - don't create new ones
            request.Requestor = originalRequestor;
            request.RequestorId = originalRequestorId;
        }

        // when new request is created
        if (request.Id == 0)
        {
            // use current date time if not provided from LIS
            request.SampleCollectionDate ??= dateTime.Now;
            request.RequestDate ??= dateTime.Now;
        }
        else
        {
            // preserve original dates when updates are coming from LIS
            request.SampleCollectionDate = sampleCollectionDate;
            request.RequestDate = requestDate;
        }

        // changes are not allowed if started. Keep the old values
        if (started)
        {
            request.SampleTubeType = sampleTubeType;
        }
        else
        {
            request.SampleTubeType = sampleTube.TubeDefinitionId;
        }

        request.PatientAge = requestSettings.Value.IsPatientInformationEnabled
            ? GetPatientAge(importedPatient, importedSample.SampleCollectionDate, request.RequestDate)
            : null;

        // Only assign patient if enabled
        if (patient != null && requestSettings.Value.IsPatientInformationEnabled)
        {
            request.Patient = patient;
        }

        // Only assign requestor if enabled
        if (requestor != null && requestSettings.Value.IsRequestorInformationEnabled)
        {
            request.Requestor = requestor;
        }

        var technologies = await HandleImportedSampleTestOrders(request, importedSample.TestOrders);

        SetSampleVolumeCheck(request, requestSettings.Value.SampleVolumeCheckSettings, technologies);

        request.UpdateRequestState();

        return request;
    }

    private static void SetSampleVolumeCheck(Domain.Request request, SampleVolumeCheckSettings settings, List<AssayTechnology> technologies)
    {
        // Update of requests does not affect the sample volume check
        if (request.Id > 0)
        {
            return;
        }

        if (settings.UseSampleVolumeCheckPediatric && request.PatientAge.HasValue)
        {
            if(request.PatientAge.Value <= settings.Age)
            {
                request.SampleVolumeCheck = Map(settings.SelectedSampleVolumeCheckPediatric);
                return;
            }
        }

        if (settings.UseSampleVolumeCheck)
        {
            if(settings.OnlyForImmunoCap && !technologies.Contains(AssayTechnology.ImmunoCap) && !technologies.Contains(AssayTechnology.ImmunoCapIgG))
            {
                request.SampleVolumeCheck = SampleVolumeCheck.Off;
                return;
            }

            request.SampleVolumeCheck = Map(settings.SelectedSampleVolumeCheck);
            return;
        }
        
        request.SampleVolumeCheck = SampleVolumeCheck.Off;
    }

    private static SampleVolumeCheck Map(SettingsSampleVolumeCheck settingsSampleVolumeCheck)
    {
        return settingsSampleVolumeCheck switch {
            SettingsSampleVolumeCheck.Off => SampleVolumeCheck.Off,
            SettingsSampleVolumeCheck.OnRunAccordingToTestOrder => SampleVolumeCheck.OnRunAccordingToTestOrder,
            SettingsSampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow => SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow,
            _ => throw new ArgumentOutOfRangeException(nameof(settingsSampleVolumeCheck), settingsSampleVolumeCheck, null)
        };
    }

    private static SampleVolumeCheck Map(SettingsSampleVolumeCheckPediatric pediatricSampleVolumeCheck)
    {
        return pediatricSampleVolumeCheck switch
        {
            SettingsSampleVolumeCheckPediatric.OnRunAccordingToTestOrder => SampleVolumeCheck.OnRunAccordingToTestOrder,
            SettingsSampleVolumeCheckPediatric.OnRunNoTestsWhenVolumeTooLow => SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow,
            _ => SampleVolumeCheck.Off
        };
    }

    private TubeType? GetTubeDefinition(LisSample importedSample)
    {
        const string normalTubeType = "N";
        const string pediatricTubeType = "C";

        var settings = tubeSettings.Value;
        var tubeType = string.IsNullOrWhiteSpace(importedSample.TubeType) ? normalTubeType : importedSample.TubeType;

        var configuredTubeValue = tubeType switch
        {
            normalTubeType => settings.NormalTubeDefinitionId,
            pediatricTubeType => settings.PediatricTubeDefinitionId,
            _ => tubeType
        };

        return int.TryParse(configuredTubeValue, out var tubeDefinitionId)
            ? settings.TubeTypeList.FirstOrDefault(x => x.TubeDefinitionId == tubeDefinitionId)
            : null;
    }


    /// <summary>
    /// Get sample from database, create if needed
    /// </summary>
    private async Task<Domain.Request> GetRequest(LisSample importedSample, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRequestIdQuery(importedSample.SampleId), cancellationToken);

        Domain.Request request;
        if (result.RequestExists)
        {
            request = await dbContext.Requests
                .Include(s => s.TestOrders)
                .ThenInclude(x => x.ReflexDefinitions)
                .Include(s => s.Requestor)
                .FirstAsync(s => s.Id == result.RequestId, cancellationToken);

            request.LastModified = dateTime.Now; // ensure modified entity state
            return request;
        }

        request = new Domain.Request
        {
            Source = RequestSource.Lis,
            TestOrders = new List<TestOrder>(),
            SampleId = importedSample.SampleId
        };

        // Check with sample module if any tubes are marked as high priority
        var markedAsHighPriority = await sender.Send(new HasSampleTubeHighPriorityQuery(new RequestUid { SampleId = request.SampleId }), cancellationToken);
        if (markedAsHighPriority)
        {
            request.HighPriority = markedAsHighPriority;
        }

        dbContext.Requests.Add(request);

        return request;
    }

    /// <summary>
    /// Handle an imported requestor
    /// </summary>
    private async Task<Requestor?> HandleImportedRequestor(LisRequestor? importedRequestor, CancellationToken cancellationToken)
    {
        Requestor? requestor = null;
        if (importedRequestor != null && !string.IsNullOrEmpty(importedRequestor.RequestorId))
        {
            requestor = await GetRequestor(importedRequestor, cancellationToken);
            RequestorMapper.UpdateRequestor(importedRequestor, requestor);
        }

        return requestor;
    }

    /// <summary>
    /// Get requestor from database, create if needed
    /// </summary>
    private async Task<Requestor> GetRequestor(LisRequestor importedRequestor, CancellationToken cancellationToken)
    {
        var requestor = importedRequestor.HasNonUniqueRequestorId
            ? _requestList.FirstOrDefault(r =>
                r.Requestor != null &&
                string.Equals(r.Requestor.RequestorId, importedRequestor.RequestorId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.Requestor.Name, importedRequestor.Name, StringComparison.OrdinalIgnoreCase))?.Requestor
            : _requestList.FirstOrDefault(r =>
                r.Requestor != null &&
                 string.Equals(r.Requestor.RequestorId, importedRequestor.RequestorId, StringComparison.OrdinalIgnoreCase))?.Requestor;

        if (requestor == null)
        {
            requestor = !importedRequestor.HasNonUniqueRequestorId
                ? await dbContext.Requestors.FirstOrDefaultAsync(r => r.RequestorId == importedRequestor.RequestorId, cancellationToken)
                : await dbContext.Requestors.FirstOrDefaultAsync(r => r.RequestorId == importedRequestor.RequestorId && r.Name == importedRequestor.Name, cancellationToken);
        }

        if (requestor == null)
        {
            requestor = new Requestor();
            await dbContext.Requestors.AddAsync(requestor, cancellationToken);
        }

        return requestor;
    }

    private async Task<List<AssayTechnology>> HandleImportedSampleTestOrders(Domain.Request request, List<LisTestOrder> importedTestOrders)
    {
        // collect all valid imported test orders
        var isPanelExpansionEnabled = importedTestOrders.Any(x => x.PanelExpansionEnabled);
        var isForceUniqueLabTestNamesEnabled = importedTestOrders.Any(x => x.ForceUniqueLaboratoryTestNames);
        var importedTestOrderCandidates = await CollectValidTestOrderCandidates(request, importedTestOrders, isPanelExpansionEnabled, isForceUniqueLabTestNamesEnabled);

        // Assign priority order to each non panel individual candidates and then sort based on priority based sorting rule
        var useRequestPriorityA = importedTestOrders.Any(x => x.UseRequestPriorityA);
        var useRequestPriorityB = importedTestOrders.Any(x => x.UseRequestPriorityB);

        if (useRequestPriorityA || useRequestPriorityB)
        {
            for (var i = 0; i < importedTestOrderCandidates.Count; i++)
            {
                var candidate = importedTestOrderCandidates[i];

                if (!candidate.IsExpandedFromPanel)
                {
                    importedTestOrderCandidates[i] = SetPriorityToTestOrderCandidate(candidate, useRequestPriorityA, useRequestPriorityB);
                }
            }

            importedTestOrderCandidates = SetSortOrderToTestOrderCandidate(request, importedTestOrderCandidates);
        }
        else
        {
            importedTestOrderCandidates = SetDefaultSortOrderToTestOrderCandidate(importedTestOrderCandidates);
        }

        var technologies = importedTestOrderCandidates.Where(x => x.MethodTestData != null).Select(x => x.MethodTestData!.Technology).Distinct().ToList();

        var processedTestOrders = await ProcessTestOrderCandidates(request, importedTestOrderCandidates);

        // Panel recognition
        var panelRecogFromOneMessage = importedTestOrders.Any(x => x.AutoPanelRecognitionFromOneMessage);
        var panelRecogFromMultipleMessage = importedTestOrders.Any(x => x.AutoPanelRecognitionFromMultipleMessage);
        await HandlePanelRecognition(request, processedTestOrders, panelRecogFromOneMessage, panelRecogFromMultipleMessage);

        return technologies;
    }

    private async Task<List<ImportedTestOrderCandidate>> CollectValidTestOrderCandidates(Domain.Request request, List<LisTestOrder> importedTestOrders, bool isPanelExpansionEnabled, bool isForceUniqueLaboratoryTestNamesEnabled)
    {
        var candidates = new List<ImportedTestOrderCandidate>();
        var failedTests = new List<(string MethodName, string TestName)>();

        for (var importedIndex = 0; importedIndex < importedTestOrders.Count; importedIndex++)
        {
            var importedTestOrder = importedTestOrders[importedIndex];

            // If method name is empty, check if test name matches a panel name when PanelExpansion setting is enabled
            if (string.IsNullOrEmpty(importedTestOrder.MethodName) && isPanelExpansionEnabled)
            {
                var panel = await GetPanelByName(importedTestOrder.TestName);

                if (panel is { Enabled: true, Tests.Count: > 0 })
                {
                    var expanded = await GetPanelExpandedTestsBySortOrder(panel, importedTestOrder);

                    foreach (var (panelImportedTestOrder, orderWithinPanel) in expanded)
                    {
                        var panelMethodTestData = await GetMethodTestData(panelImportedTestOrder.TestName, panelImportedTestOrder.MethodName, forceUniqueLaboratoryTestNames: false);

                        var isPanelImportedTestOrderValid = await IsValidTestOrder(request, panelImportedTestOrder, panelMethodTestData, failedTests);

                        candidates.Add(new ImportedTestOrderCandidate(
                            ImportedTestOrder: panelImportedTestOrder,
                            MethodTestData: panelMethodTestData,
                            OrderInImportedMessage: importedIndex + 1,
                            IsExpandedFromPanel: true,
                            PanelId: panel.Id,
                            OrderWithinPanel: orderWithinPanel,
                            IsValid: isPanelImportedTestOrderValid,
                            IsPrioritized: default,
                            PriorityOrder: default,
                            IsExisting: default
                        ));
                    }

                    continue;
                }
            }

            var methodTestData = await GetMethodTestData(importedTestOrder.TestName, importedTestOrder.MethodName, isForceUniqueLaboratoryTestNamesEnabled);

            var isImportedTestOrderValid = await IsValidTestOrder(request, importedTestOrder, methodTestData, failedTests);

            candidates.Add(new ImportedTestOrderCandidate(
                    ImportedTestOrder: importedTestOrder,
                    MethodTestData: methodTestData,
                    OrderInImportedMessage: importedIndex + 1,
                    PanelId: null,
                    IsExpandedFromPanel: false,
                    OrderWithinPanel: 0,
                    IsValid: isImportedTestOrderValid,
                    IsPrioritized: default,
                    PriorityOrder: default,
                    IsExisting: default
            ));

        }

        if (failedTests.Count > 0)
        {
            var groupedByMethod = failedTests
                .GroupBy(t => t.MethodName)
                .Select(g => $"{g.Key} ({string.Join(", ", g.Select(t => t.TestName))})");
            var failedTestsList = string.Join(Environment.NewLine, groupedByMethod);

            await SendPharosError(PharosErrorCodes.UnknownTestReceivedFromLis, ErrorClassification.Error, request.SampleId, failedTestsList);
        }

        var validCandidates = candidates.Where(c => c.IsValid).ToList();
        await NormalizeSamplePreDilution(request, validCandidates);

        return validCandidates;
    }

    private async Task<List<(LisTestOrder Test, int? OrderWithinPanel)>> GetPanelExpandedTestsBySortOrder(Panel panel, LisTestOrder importedTestOrder)
    {
        var sortedPanelTests = panel.Tests.OrderBy(t => t.SortOrder ?? int.MaxValue).ToList();

        var expandedTests = new List<(LisTestOrder Test, int? OrderWithinPanel)>();

        for (var i = 0; i < sortedPanelTests.Count; i++)
        {
            var panelTest = sortedPanelTests[i];
            var laboratoryTestName = await methodsService.GetMethodTest(
                panelTest.MethodTest.Method.MethodName,
                panelTest.MethodTest.TestName,
                methodTest => methodTest.LabTestNames
                    .OrderBy(x => x.SortOrder)
                    .Select(x => x.TestName)
                    .FirstOrDefault());

            var expandedTestOrder = importedTestOrder with
            {
                TestName = laboratoryTestName ?? string.Empty,
                MethodName = panelTest.MethodTest.Method.MethodName,
                NumberOfReplicates = panelTest.Replicates,
                InstrumentDilutionFactor = panelTest.InstrumentDilutionFactor
            };

            expandedTests.Add((expandedTestOrder, panelTest.SortOrder));
        }

        return expandedTests;
    }

    private async Task<bool> IsValidTestOrder(Domain.Request request, LisTestOrder importedTestOrder, MethodTestData? methodTestData, List<(string MethodName, string TestName)> failedTests)
    {
        if (methodTestData == null)
        {
            logger.LogError("Unknown test received from LIS or test/method is disabled: Method = {LabMethodName}, Test = {LabTestName}.", importedTestOrder.MethodName, importedTestOrder.TestName);

            failedTests.Add((importedTestOrder.MethodName ?? "N/A", importedTestOrder.TestName ?? "N/A"));

            return false;
        }

        request.TestOrders ??= new List<TestOrder>();

        var existingNotStarted = request.GetNotStartedTest(methodTestData.MethodName, methodTestData.TestName);

        var preDilutionFactor = GetPreDilutionFactor(importedTestOrder, methodTestData);
        if (request.Id != 0 && preDilutionFactor != request.PreDilutionFactor)
        {
            logger.LogError("Test order from LIS is skipped. Illegal dilution factors: Pre-Dilution factor = {PreDilutionFactor}, Existing sample pre-dilution factor = {ExistingPreDilutionFactor}, Sample ID = {SampleId}, Method = {MethodName}, Test = {TestName}.",
                preDilutionFactor, request.PreDilutionFactor, request.SampleId, methodTestData.MethodName, methodTestData.TestName);

            await SendPharosError(PharosErrorCodes.TestSkippedDueToIllegalDilutionFactors, ErrorClassification.Warning, request.SampleId, methodTestData.MethodName, methodTestData.TestName, preDilutionFactor.ToString(), request.PreDilutionFactor.ToString());

            return false;
        }

        var instrumentDilutionFactor = importedTestOrder.InstrumentDilutionFactor ?? methodTestData.InstrumentDilutionFactor;
        if (!IsValidDilutionFactors(methodTestData, preDilutionFactor, instrumentDilutionFactor))
        {
            logger.LogError("Test order from LIS is skipped. Illegal dilution factors: Pre-Dilution factor = {PreDilutionFactor}, Instrument dilution factor = {InstrumentDilutionFactor}, Sample ID = {SampleId}, Method = {MethodName}, Test = {TestName}.",
                preDilutionFactor, instrumentDilutionFactor, request.SampleId, methodTestData.MethodName, methodTestData.TestName);

            await SendPharosError(PharosErrorCodes.TestSkippedDueToIllegalDilutionFactors, ErrorClassification.Warning, request.SampleId, methodTestData.MethodName, methodTestData.TestName, preDilutionFactor.ToString(), instrumentDilutionFactor.ToString());

            return false;
        }

        switch (importedTestOrder.Action)
        {
            // A NewTest is valid and will be processed when at least one of these conditions is true:
            // 1. No existing test with the same method/test name exists in an active processing state 
            //    (NotStarted, Running, Paused, or any state where HasUnfinishedTestOrder == true), OR
            // 2. An existing test with the same method/test name exists in a final/completed state 
            //    (PendingApproval, Reported, Aborted, Flagged, Approved, Rejected, or ApprovedPendingReflex 
            //    where HasUnfinishedTestOrder == false), OR
            // 3. An existing NotStarted test exists that can be updated with the new test data.
            // The test is rejected only when there's a blocking duplicate in an active state AND 
            // no NotStarted test exists to update.
            case LisTestOrderAction.NewTest:

                if (request.HasUnfinishedTestOrder(methodTestData.MethodName, methodTestData.TestName) && existingNotStarted == null)
                {
                    logger.LogInformation("No ready test exists for re-test action. Test order from LIS is ignored: Sample ID = {SampleId}, Method = {MethodName}, Test = {TestName}.", request.SampleId, methodTestData.MethodName, methodTestData.TestName);
                    return false;
                }

                break;

            // ReTest is valid when at least one test with the same method/test name exists in the request 
            // (in any state: NotStarted, Running, PendingApproval, Reported, Aborted, Flagged, Approved, 
            // Rejected, etc.). Used to repeat a test for quality or clinical reasons. Rejected only when 
            // no matching test exists in the request.
            case LisTestOrderAction.ReTest:

                if (request.TestOrders.All(to => !(to.MethodName == methodTestData.MethodName && to.TestName == methodTestData.TestName)))
                {
                    logger.LogInformation("No ready test exists for re-test action. Test order from LIS is ignored: Sample ID = {SampleId}, Method = {MethodName}, Test = {TestName}.", request.SampleId, methodTestData.MethodName, methodTestData.TestName);
                    return false;
                }

                break;

            case LisTestOrderAction.CancelTest:
                throw new NotImplementedException();

            case LisTestOrderAction.QualityControl:
                throw new NotImplementedException();

            default:
                logger.LogError("Unknown action code received from LIS: Sample ID = {SampleId}, Method = {MethodName}, Test = {TestName}.", request.SampleId, methodTestData.MethodName, methodTestData.TestName);
                return false;
        }

        return true;
    }

    private async Task NormalizeSamplePreDilution(Domain.Request request, List<ImportedTestOrderCandidate> validCandidates)
    {
        if (request.Id != 0 || validCandidates.Count == 0)
        {
            return;
        }

        var distinctPreDilutionFactors = validCandidates
            .Select(GetPreDilutionFactor)
            .ToHashSet();

        request.PreDilutionFactor = distinctPreDilutionFactors.Max();

        if (distinctPreDilutionFactors.Count > 1)
        {
            logger.LogWarning("Imported tests for a sample have different predilution factors. Sample predilution factor is set to {PreDilutionFactor}. Sample ID = {SampleId}.",
                request.PreDilutionFactor, request.SampleId);

            await SendPharosError(PharosErrorCodes.PreDilutionFactorsNormalized, ErrorClassification.Warning, request.SampleId, request.PreDilutionFactor.ToString(), string.Join(", ", distinctPreDilutionFactors));
        }
    }

    private static bool IsValidDilutionFactors(MethodTestData methodTestData, int preDilutionFactor, int instrumentDilutionFactor)
    {
        if (preDilutionFactor < 1)
        {
            return false;
        }

        var validInstrumentDilutionFactors = methodTestData.Technology switch
        {
            AssayTechnology.ImmunoCap => InstrumentDilutionFactors.ImmunoCap,
            AssayTechnology.EliA => InstrumentDilutionFactors.Elia,
            AssayTechnology.ImmunoCapIgG => InstrumentDilutionFactors.ImmunoCapIgG,
            AssayTechnology.ViewAllergy => InstrumentDilutionFactors.ViewAllergy,
            _ => []
        };

        if (!validInstrumentDilutionFactors.Contains(instrumentDilutionFactor))
        {
            return false;
        }

        var defaultDilutionFactor = methodTestData.DilutionMethod == DilutionMethod.PreDiluted
            ? 1
            : methodTestData.DilutionFactor;

        var isDilutionBelowDefault = methodTestData.PreventDilutionFactorsBelowDefault && instrumentDilutionFactor * preDilutionFactor < defaultDilutionFactor;
        return !isDilutionBelowDefault;
    }

    private static int GetPreDilutionFactor(ImportedTestOrderCandidate candidate)
    {
        return GetPreDilutionFactor(candidate.ImportedTestOrder, candidate.MethodTestData);
    }

    private static int GetPreDilutionFactor(LisTestOrder importedTestOrder, MethodTestData? methodTestData)
    {
        if (importedTestOrder.PreDilutionFactor.HasValue)
        {
            return importedTestOrder.PreDilutionFactor.Value;
        }

        return methodTestData?.DilutionMethod == DilutionMethod.PreDiluted
            ? methodTestData.DilutionFactor
            : 1;
    }

    private async Task SendPharosError(int errorCode, ErrorClassification classification, params string[] parameters)
    {
        await sender.Send(new CreatePharosErrorCommand(
            errorCode,
            classification,
            null,
            parameters.ToList())).ConfigureAwait(false);
    }

    private static ImportedTestOrderCandidate SetPriorityToTestOrderCandidate(ImportedTestOrderCandidate testCandidate, bool useRequestPriorityA, bool useRequestPriorityB)
    {
        if (testCandidate.MethodTestData == null || (!useRequestPriorityA && !useRequestPriorityB))
        {
            return testCandidate;
        }

        if (useRequestPriorityA)
        {
            return testCandidate with
            {
                IsPrioritized = testCandidate.MethodTestData.IncludeInPriorityGroupA,
                PriorityOrder = testCandidate.MethodTestData.OrderInPriorityGroupA,
            };
        }
        else if (useRequestPriorityB)
        {
            return testCandidate with
            {
                IsPrioritized = testCandidate.MethodTestData.IncludeInPriorityGroupB,
                PriorityOrder = testCandidate.MethodTestData.OrderInPriorityGroupB,
            };
        }

        return testCandidate;
    }

    private static List<ImportedTestOrderCandidate> SetDefaultSortOrderToTestOrderCandidate(List<ImportedTestOrderCandidate> candidates)
    {
        var result = new List<ImportedTestOrderCandidate>();

        if (candidates is null || candidates.Count == 0)
        {
            return result;
        }

        var startOrder = 1;
        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            var isExistsInResult = result.Any(r => r.MethodTestData!.TestName == candidate.MethodTestData!.TestName && r.MethodTestData.MethodName == candidate.MethodTestData.MethodName);

            if (!isExistsInResult)
            {
                candidates[i] = candidates[i] with { SortOrder = startOrder };
                result.Add(candidates[i]);
                startOrder++;
            }
        }

        return result;
    }

    private static List<ImportedTestOrderCandidate> SetSortOrderToTestOrderCandidate(Domain.Request request, List<ImportedTestOrderCandidate> validCandidates)
    {
        var result = new List<ImportedTestOrderCandidate>();

        if (validCandidates is null || validCandidates.Count == 0)
        {
            return result;
        }

        static int NormalizeOrder(int? value) => (!value.HasValue || value.Value == 0) ? int.MaxValue : value.Value;

        var existingTestOrders = request.TestOrders;

        for (var i = 0; i < validCandidates.Count; i++)
        {
            var methodTestData = validCandidates[i].MethodTestData;

            if (methodTestData is not null)
            {
                var notStartedTest = methodTestData is null ? null : request.GetNotStartedTest(methodTestData.MethodName, methodTestData.TestName);

                if (notStartedTest != null)
                {
                    validCandidates[i] = validCandidates[i] with { IsExisting = true, SortOrder = notStartedTest.TestSortOrder, IsPrioritized = false, PriorityOrder = 0 };
                }
            }
        }

        var sortedCandidates = validCandidates.OrderBy(c => NormalizeOrder(c.PriorityOrder)).ThenBy(c => c.OrderInImportedMessage).ToList();

        var startIndex = 1;

        // If there is no prioritized test, assign sort order after the max existing sort order to the non-existing tests from sortedCandidates
        if (!sortedCandidates.Any(c => c.IsPrioritized))
        {
            startIndex = existingTestOrders.Count == 0 ? 1 : existingTestOrders.Max(x => x.TestSortOrder) + 1;

            for (var i = 0; i < sortedCandidates.Count; i++)
            {
                var candidate = sortedCandidates[i];

                if (!candidate.IsExisting)
                {
                    var isExistsInResult = result.Any(r => r.MethodTestData!.TestName == candidate.MethodTestData!.TestName && r.MethodTestData.MethodName == candidate.MethodTestData.MethodName);

                    if (!isExistsInResult)
                    {
                        sortedCandidates[i] = candidate with { SortOrder = startIndex };
                        startIndex++;
                        result.Add(sortedCandidates[i]);
                    }

                }
            }

            return sortedCandidates;
        }


        // A- Set SortOrder for prioritized tests starting from index 1
        var prioritizedCandidates = sortedCandidates.Where(c => c.IsPrioritized).ToList();

        for (var i = 0; i < prioritizedCandidates.Count; i++)
        {
            var candidate = prioritizedCandidates[i];
            var isExistsInResult = result.Any(r => r.MethodTestData!.TestName == candidate.MethodTestData!.TestName && r.MethodTestData.MethodName == candidate.MethodTestData.MethodName);

            if (!isExistsInResult)
            {
                prioritizedCandidates[i] = prioritizedCandidates[i] with { SortOrder = startIndex };
                startIndex++;
                result.Add(prioritizedCandidates[i]);
            }
        }

        // B- Update TestSortOrder for existing test orders in database and sync SortOrder in respective candidate
        var existingCandidates = sortedCandidates.Where(c => c.IsExisting).ToList();
        existingTestOrders = existingTestOrders.OrderBy(x => NormalizeOrder(x.TestSortOrder)).ToList();

        for (var i = 0; i < existingTestOrders.Count; i++)
        {
            existingTestOrders[i].TestSortOrder = startIndex++;

            // Find and update the corresponding candidate
            var candidateIndex = existingCandidates.FindIndex(c =>
                c.MethodTestData != null &&
                c.MethodTestData.TestName == existingTestOrders[i].TestName &&
                c.MethodTestData.MethodName == existingTestOrders[i].MethodName);

            if (candidateIndex >= 0)
            {
                var candidate = existingCandidates[candidateIndex];
                var isExistsInResult = result.Any(r => r.MethodTestData!.TestName == candidate.MethodTestData!.TestName && r.MethodTestData.MethodName == candidate.MethodTestData.MethodName);

                if (!isExistsInResult)
                {
                    existingCandidates[candidateIndex] = existingCandidates[candidateIndex] with { SortOrder = existingTestOrders[i].TestSortOrder };
                    result.Add(existingCandidates[candidateIndex]);
                }
            }
        }

        // C- Set FinalSortOrder for remaining tests continuing after existing tests
        var remaining = sortedCandidates.Where(c => !c.IsPrioritized && !c.IsExisting).ToList();

        for (var i = 0; i < remaining.Count; i++)
        {
            var candidate = remaining[i];
            var isExistsInResult = result.Any(r => r.MethodTestData!.TestName == candidate.MethodTestData!.TestName && r.MethodTestData.MethodName == candidate.MethodTestData.MethodName);

            if (!isExistsInResult)
            {
                remaining[i] = remaining[i] with { SortOrder = startIndex };
                startIndex++;
                result.Add(remaining[i]);
            }

        }

        return result;
    }

    private async Task<List<TestOrder>> ProcessTestOrderCandidates(Domain.Request request, List<ImportedTestOrderCandidate> sortedCandidates)
    {
        var processedTestOrders = new List<TestOrder>();

        var reflexGroups = sortedCandidates
            .Where(c => !string.IsNullOrEmpty(c.ImportedTestOrder.ReflexName))
            .GroupBy(c => c.ImportedTestOrder.ReflexName)
            .ToList();

        var independentCandidates = sortedCandidates
            .Where(c => string.IsNullOrEmpty(c.ImportedTestOrder.ReflexName))
            .ToList();

        foreach (var reflexGroup in reflexGroups)
        {
            var reflexName = reflexGroup.Key;
            var groupCandidates = reflexGroup.OrderBy(c => c.OrderInImportedMessage).ToList();
            var triggerCandidate = groupCandidates.First();

            // Case 1: If TRIGGER has SuppressReflex, create trigger as independent WITHOUT any reflex definitions
            if (triggerCandidate.ImportedTestOrder.SuppressReflex)
            {
                logger.LogInformation("Trigger test has SuppressReflex enabled. Creating trigger test as independent without reflex definition. ReflexName = {ReflexName}, Sample ID = {SampleId}, Trigger Test = {TestName}/{MethodName}",
                    reflexName, request.SampleId, triggerCandidate.MethodTestData?.TestName, triggerCandidate.MethodTestData?.MethodName);

                // Create trigger test directly without checking for default reflex definitions
                var independentTestOrder = await CreateOrUpdateTestOrder(request, triggerCandidate, null);
                if (independentTestOrder != null)
                {
                    processedTestOrders.Add(independentTestOrder);
                }
                continue;
            }

            // Case 2: Trigger does NOT have SuppressReflex - check requestor-level suppression
            if (!IsReflexEnabledForRequestor(request.Requestor))
            {
                logger.LogInformation("Reflex testing disabled for requestor. Creating trigger test as independent without reflex definition. ReflexName = {ReflexName}, Sample ID = {SampleId}, RequestorId = {RequestorId}",
                    reflexName, request.SampleId, request.Requestor?.RequestorId);

                // Create trigger test directly without checking for default reflex definitions
                var independentTestOrder = await CreateOrUpdateTestOrder(request, triggerCandidate, null);
                if (independentTestOrder != null)
                {
                    processedTestOrders.Add(independentTestOrder);
                }
                continue;
            }

            // Case 3: Process reflex group - filter out follow-ups that have SuppressReflex enabled
            var followUpCandidates = groupCandidates.Skip(1).ToList();
            var validFollowUpCandidates = followUpCandidates.Where(c => !c.ImportedTestOrder.SuppressReflex).ToList();

            if (followUpCandidates.Count > validFollowUpCandidates.Count)
            {
                var suppressedCount = followUpCandidates.Count - validFollowUpCandidates.Count;
                logger.LogInformation("Suppressing {SuppressedCount} follow-up test(s) with SuppressReflex flag. ReflexName = {ReflexName}, Sample ID = {SampleId}",
                    suppressedCount, reflexName, request.SampleId);
            }

            // Process reflex group with valid follow-ups only
            var candidatesForProcessing = new List<ImportedTestOrderCandidate> { triggerCandidate };
            candidatesForProcessing.AddRange(validFollowUpCandidates);

            var testOrder = await ProcessReflexGroup(request, reflexName, candidatesForProcessing);
            if (testOrder != null)
            {
                processedTestOrders.Add(testOrder);
            }
        }

        foreach (var candidate in independentCandidates)
        {
            var testOrder = await HandleIndependentTestOrder(request, candidate);
            if (testOrder != null)
            {
                processedTestOrders.Add(testOrder);
            }
        }

        return processedTestOrders;
    }

    private static bool IsReflexEnabledForRequestor(Requestor? requestor)
    {
        return requestor?.ReflexEnabled ?? true;
    }

    private async Task<TestOrder?> ProcessReflexGroup(Domain.Request request, string reflexName, List<ImportedTestOrderCandidate> groupCandidates)
    {
        var triggerCandidate = groupCandidates.First();
        var followUpCandidates = groupCandidates.Skip(1).ToList();

        if (triggerCandidate.MethodTestData == null)
        {
            logger.LogError("Reflex group skipped: Trigger test has no method test data. ReflexName = {ReflexName}, Sample ID = {SampleId}",
                reflexName, request.SampleId);
            return null;
        }

        var reflexDefinition = await sender.Send(
            new GetReflexDefinitionsForReflexNameQuery(
                reflexName,
                triggerCandidate.MethodTestData.TestName,
                triggerCandidate.MethodTestData.MethodName,
                request.Requestor?.RequestorId))
            .ConfigureAwait(false);

        if (reflexDefinition == null)
        {
            logger.LogError("Reflex group skipped: No active reflex definition matched the reflex name, trigger test, and requestor. ReflexName = {ReflexName}, Sample ID = {SampleId}, Trigger Test = {TestName}/{MethodName}",
                reflexName, request.SampleId, triggerCandidate.MethodTestData.TestName, triggerCandidate.MethodTestData.MethodName);
            return null;
        }

        foreach (var followUp in followUpCandidates)
        {
            if (followUp.ImportedTestOrder.ReflexName != reflexName)
            {
                logger.LogError("Reflex group skipped: Follow-up test has mismatched ReflexName. Expected = {ExpectedReflexName}, Actual = {ActualReflexName}, Sample ID = {SampleId}",
                    reflexName, followUp.ImportedTestOrder.ReflexName, request.SampleId);
                return null;
            }
        }

        var testOrder = await CreateOrUpdateTestOrder(request, triggerCandidate, null);
        if (testOrder == null)
        {
            return null;
        }

        if (followUpCandidates.Count == 0)
        {
            testOrder.ReflexDefinitions = new List<ReflexDefinition>
            {
                ReflexDefinitionMapper.CloneReflexDefinition(reflexDefinition)
            };
        }
        else
        {
            var clonedDefinition = ReflexDefinitionMapper.CloneReflexDefinition(reflexDefinition);
            clonedDefinition.ReflexTests.Clear();

            foreach (var followUpCandidate in followUpCandidates)
            {
                if (followUpCandidate.MethodTestData == null)
                {
                    continue;
                }

                var reflexTest = new ReflexTest
                {
                    MethodName = followUpCandidate.MethodTestData.MethodName,
                    TestName = followUpCandidate.MethodTestData.TestName,
                    NumberOfReplicates = followUpCandidate.ImportedTestOrder.NumberOfReplicates ?? followUpCandidate.MethodTestData.NumberOfReplicates,
                    InstrumentDilutionFactor = followUpCandidate.ImportedTestOrder.InstrumentDilutionFactor ?? followUpCandidate.MethodTestData.InstrumentDilutionFactor,
                    TestSortOrder = followUpCandidate.SortOrder
                };

                clonedDefinition.ReflexTests.Add(reflexTest);
            }

            testOrder.ReflexDefinitions = new List<ReflexDefinition> { clonedDefinition };
        }

        return testOrder;
    }

    private async Task<TestOrder?> HandleIndependentTestOrder(Domain.Request request, ImportedTestOrderCandidate candidate)
    {
        if (candidate.MethodTestData == null)
        {
            return null;
        }

        var testOrder = await CreateOrUpdateTestOrder(request, candidate, candidate.PanelId);
        if (testOrder == null)
        {
            return null;
        }

        var reflexDefinitions = await sender.Send(
            new GetReflexDefinitionsForTestQuery(testOrder.TestName, testOrder.MethodName))
            .ConfigureAwait(false);

        if (reflexDefinitions != null)
        {
            testOrder.ReflexDefinitions = ReflexDefinitionMapper.CloneReflexDefinitions(reflexDefinitions);
        }

        return testOrder;
    }

    private async Task HandlePanelRecognition(Domain.Request request, List<TestOrder> testOrders, bool panelRecogFromOneMessage, bool panelRecogFromMultipleMessage)
    {
        if (!(panelRecogFromOneMessage || panelRecogFromMultipleMessage))
        {
            return;
        }

        // Include all test orders without a panel, including tests with reflex definitions.
        var testOrdersForPanelRecognition = testOrders.Where(test => test.TestPanelIds.Count == 0).ToList();

        if (panelRecogFromMultipleMessage)
        {
            // When "From multiple messages" is enabled, only tests with NotStarted or Scheduled state are eligible for panel recognition
            var existingTestOrdersWithoutPanel = request.TestOrders
                .Where(to => to.TestPanelIds.Count == 0 && (to.State == TestOrderState.NotStarted || to.State == TestOrderState.Scheduled))
                .ToList();

            testOrdersForPanelRecognition.AddRange(existingTestOrdersWithoutPanel);
        }

        if (testOrdersForPanelRecognition.Count == 0)
        {
            return;
        }

        var methodTestsForPanelRecognition = testOrdersForPanelRecognition.Select(t => (t.MethodName, t.TestName)).ToHashSet();
        var allPanelForRecognitionDtos = await sender.Send(new GetPanelsForTestsQuery(methodTestsForPanelRecognition));


        var completeMatchingPanelForRecognitionDtos = new List<PanelForRecognitionDto>();

        foreach (var panelDto in allPanelForRecognitionDtos)
        {
            // check all tests of this panel is available in test orders from LIS message (methodTestsForPanelRecognition)
            var isCompleteMatch = panelDto.Tests.All(pt => methodTestsForPanelRecognition.Contains(pt));

            if (isCompleteMatch)
            {
                completeMatchingPanelForRecognitionDtos.Add(panelDto);
            }
        }

        if (completeMatchingPanelForRecognitionDtos.Count == 0)
        {
            return;
        }

        // Panel with the highest number of matching tests, if tie, alphabetically lowest panel name
        completeMatchingPanelForRecognitionDtos = completeMatchingPanelForRecognitionDtos.OrderByDescending(p => p.Tests.Count).ThenBy(p => p.PanelName, StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var completeMatchingPanel in completeMatchingPanelForRecognitionDtos)
        {
            testOrdersForPanelRecognition = testOrdersForPanelRecognition.Where(to => to.TestPanelIds.Count == 0).ToList();

            if (testOrdersForPanelRecognition.Count == 0)
            {
                return;
            }

            var testsForPanelRecognitionKeys = testOrdersForPanelRecognition.Select(t => (t.MethodName, t.TestName)).ToHashSet();

            var completeMatchingPanelTestKeys = completeMatchingPanel.Tests.ToHashSet();

            var allPanelTestsPresent = completeMatchingPanelTestKeys.All(pt => testsForPanelRecognitionKeys.Contains(pt));

            if (!allPanelTestsPresent)
            {
                continue;
            }

            // Get only the test orders that are part of this panel
            var testOrdersToAssignPanel = testOrdersForPanelRecognition.Where(to => completeMatchingPanelTestKeys.Contains((to.MethodName, to.TestName))).ToList();

            foreach (var testOrder in testOrdersToAssignPanel)
            {
                AssignPanelToTestOrder(testOrder, completeMatchingPanel.Id);
            }
        }

    }

    /// <summary>
    /// Create or update a test order from a candidate (without handling reflex definitions)
    /// </summary>
    /// <returns>The created or updated test order, or null if the test order was not processed</returns>
    private async Task<TestOrder?> CreateOrUpdateTestOrder(Domain.Request request, ImportedTestOrderCandidate importedTestOrderCandidate, int? panelId)
    {
        if (importedTestOrderCandidate.MethodTestData == null)
        {
            return null;
        }

        var methodName = importedTestOrderCandidate.MethodTestData.MethodName;
        var testName = importedTestOrderCandidate.MethodTestData.TestName;
        var labTestName = importedTestOrderCandidate.MethodTestData.LabTestName;
        var importedInstrumentDilutionFactor = importedTestOrderCandidate.ImportedTestOrder.InstrumentDilutionFactor;
        var methodTestInstrumentDilutionFactor = importedTestOrderCandidate.MethodTestData.InstrumentDilutionFactor;
        var importedNumberOfReplicates = importedTestOrderCandidate.ImportedTestOrder.NumberOfReplicates;
        var methodTestNumberOfReplicates = importedTestOrderCandidate.MethodTestData.NumberOfReplicates;

        // Check if there is an existing not started test
        var testOrder = request.GetNotStartedTest(methodName, testName);

        if (testOrder != null)
        {
            // If the existing not started test order exists, update it either with DB values or
            // with LIS values depending on the import settings
            testOrder.InstrumentDilutionFactor = importedInstrumentDilutionFactor ?? methodTestInstrumentDilutionFactor;
            testOrder.NumberOfReplicates = importedNumberOfReplicates ?? methodTestNumberOfReplicates;
            testOrder.TestSortOrder = importedTestOrderCandidate.SortOrder;
            testOrder.EntityState = EntityState.Modified;
        }
        else
        {
            // No test to override, then add a new test
            testOrder = new TestOrder
            {
                EntityState = EntityState.Added,
                TestName = testName,
                MethodName = methodName,
                LabTestName = labTestName,
                TestSortOrder = importedTestOrderCandidate.SortOrder,
                InstrumentDilutionFactor = importedInstrumentDilutionFactor ?? methodTestInstrumentDilutionFactor,
                NumberOfReplicates = importedNumberOfReplicates ?? methodTestNumberOfReplicates,
                Uid = Guid.NewGuid(),
                LisChannelId = _lisChannelId
            };

            dbContext.TestOrders.Add(testOrder);
            request.TestOrders.Add(testOrder);
        }

        AssignPanelToTestOrder(testOrder, panelId);

        return testOrder;
    }

    private static void AssignPanelToTestOrder(TestOrder testOrder, int? panelId)
    {
        if (panelId.HasValue && testOrder.TestPanelIds.Count == 0)
        {
            testOrder.TestPanelIds.Add(panelId.Value);
        }
    }

    /// <summary>
    /// Tell the world about it
    /// </summary>
    private void AddDomainEvents()
    {
        foreach (var sample in dbContext.Requests.Local)
        {
            var state = dbContext.Requests.Entry(sample).State;
            if (state == Microsoft.EntityFrameworkCore.EntityState.Added)
            {
                sample.AddDomainEvent(new RequestCreatedEvent(sample));
            }
            else if (state == Microsoft.EntityFrameworkCore.EntityState.Modified)
            {
                sample.AddDomainEvent(new RequestUpdatedEvent(sample.Id, sample.GetRequestUid(), sample.TestOrders.ToList()));
            }
        }
    }

    /// <summary>
    /// Gets the patient age, either from LIS or by calculating from birth date 
    /// based on the CalculateAge flag.
    /// </summary>
    /// <param name="importedPatient">The patient information from LIS</param>
    /// <param name="sampleCollectionDate">The date when the sample was collected from the patient</param>
    /// <param name="requestDate">The date when the request to run a test was ordered by a lab</param>
    /// <returns>The patient's age, or null if it cannot be determined</returns>
    private int? GetPatientAge(LisPatient importedPatient, DateTimeOffset? sampleCollectionDate, DateTimeOffset? requestDate)
    {
        if (importedPatient.PatientAge.HasValue)
        {
            return importedPatient.PatientAge;
        }

        if (!importedPatient.CalculateAge || !importedPatient.BirthDate.HasValue)
        {
            return null;
        }

        var referenceDate = sampleCollectionDate ?? requestDate ?? dateTime.Now;
        var birthDate = importedPatient.BirthDate.Value;
        var ageReferenceDate = DateOnly.FromDateTime(referenceDate.DateTime);

        if (birthDate > ageReferenceDate)
        {
            return null;
        }

        var age = referenceDate.Year - birthDate.Year;

        if (referenceDate.Month < birthDate.Month ||
            (referenceDate.Month == birthDate.Month && referenceDate.Day < birthDate.Day))
        {
            age--;
        }

        return age;
    }

    private async Task<MethodTestData?> GetMethodTestData(string testName, string methodName, bool forceUniqueLaboratoryTestNames)
    {
        var methodTest = await methodsService.GetMethodTestByLabName(methodName, testName, x => new
        {
            x.TestName,
            x.Method.MethodName,
            x.InstrumentDilutionFactor,
            x.Replicates,
            LabTestName = testName,
            Priority = x.Priority == null ? null : new MethodTestPriority
            {
                IncludeInPriorityGroupA = x.Priority.IncludeInPriorityGroupA,
                OrderInPriorityGroupA = x.Priority.OrderInPriorityGroupA,
                IncludeInPriorityGroupB = x.Priority.IncludeInPriorityGroupB,
                OrderInPriorityGroupB = x.Priority.OrderInPriorityGroupB
            },
            x.DilutionFactor,
            x.DilutionMethod,
            x.Method.Technology,
            x.Method.PreventDilutionFactorsBelowDefault
        }, forceUniqueLaboratoryTestNames);

        if (methodTest != null)
        {
            return new MethodTestData(
                TestName: methodTest.TestName,
                MethodName: methodTest.MethodName,
                InstrumentDilutionFactor: methodTest.InstrumentDilutionFactor,
                NumberOfReplicates: methodTest.Replicates,
                LabTestName: methodTest.LabTestName,
                IncludeInPriorityGroupA: methodTest.Priority?.IncludeInPriorityGroupA ?? false,
                OrderInPriorityGroupA: methodTest.Priority?.OrderInPriorityGroupA ?? default,
                IncludeInPriorityGroupB: methodTest.Priority?.IncludeInPriorityGroupB ?? false,
                OrderInPriorityGroupB: methodTest.Priority?.OrderInPriorityGroupB ?? default,
                methodTest.DilutionFactor,
                methodTest.DilutionMethod,
                methodTest.Technology,
                methodTest.PreventDilutionFactorsBelowDefault
            );
        }

        return null;
    }

    private async Task<Panel?> GetPanelByName(string panelName)
    {
        return await sender.Send(new GetPanelByNameQuery(panelName));
    }

    private sealed record ImportedTestOrderCandidate(
       LisTestOrder ImportedTestOrder,
       MethodTestData? MethodTestData,
       int OrderInImportedMessage,
       bool IsPrioritized,
       int? PriorityOrder,
       bool IsExpandedFromPanel,
       int? PanelId,
       int? OrderWithinPanel,
       bool IsExisting,
       bool IsValid = true,
       int SortOrder = 0
    );

    private sealed record MethodTestData(
        string TestName,
        string MethodName,
        int InstrumentDilutionFactor,
        int NumberOfReplicates,
        string? LabTestName,
        bool IncludeInPriorityGroupA,
        int OrderInPriorityGroupA,
        bool IncludeInPriorityGroupB,
        int OrderInPriorityGroupB,
        int DilutionFactor,
        DilutionMethod DilutionMethod,
        AssayTechnology Technology,
        bool PreventDilutionFactorsBelowDefault);
}
