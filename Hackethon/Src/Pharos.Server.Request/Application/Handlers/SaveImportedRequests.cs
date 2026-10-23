using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Parameters;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure;
using Pharos.Server.Request.Application.Commands.CreateUpdateRequest;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Models;
using Pharos.Server.Request.Domain;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;

namespace Pharos.Server.Request.Application.Handlers;

internal class SaveImportedRequests(
    IRequestDbContext dbContext,
    IOptionsSnapshot<TubeTypesSettings> tubeSettings,
    IMethodsService methodService,
    IMediator mediator,
    IDateTime dateTime,
    ITestDisplayNameProvider displayNameProvider,
    ILogger<SaveImportedRequests> logger) : ISaveImportedRequests
{
    public async Task Save(ImportedRequests requests, CancellationToken cancellationToken)
    {
        if (await ValidateRequests(requests))
        {
            // TODO: we need to do a pre-scan before we save anything to the database to see if any trays are loaded to the instrument
            await SaveRequests(requests, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> ValidateRequests(ImportedRequests requests)
    {
        var result = true;

        var usedCustomTestIds = new List<string>();

        foreach (var sample in requests.Samples)
        {
            if (!ValidateSample(sample))
            {
                result = false;
                break;
            }

            foreach (var test in sample.Tests)
            {
                if (!await ValidateImportedTest(sample, test, usedCustomTestIds))
                {
                    result = false;
                    break;
                }
            }
        }

        return result;
    }

    private bool ValidateSample(ImportedSample sample)
    {
        if(!SampleRequestIdValidator.IsValidSampleId(sample.SampleId))
        {
            throw new ArgumentException("Request import: Sample ID contains invalid characters", sample.SampleId);
        }

        if (sample.PreDilutionFactor != null && (sample.PreDilutionFactor.Value < 1 || sample.PreDilutionFactor.Value > 10000))
        {
            throw new ArgumentOutOfRangeException(sample.PreDilutionFactor.Value.ToString(), "Request import: Illegal pre-dilution factor");
        }

        if (sample.TubeType.HasValue)
        {
            var tubeTypes = GetTubeTypes();
            if (tubeTypes.All(t => t.TubeDefinitionId != sample.TubeType.Value))
            {
                throw new ArgumentOutOfRangeException(sample.TubeType.Value.ToString(), "Request import: Unknown tube type");
            }
        }

        // Check tube position
        if (!string.IsNullOrEmpty(sample.TubePosition) && GetSampleTrayPosition(sample.TubePosition) == 0)
        {
            throw new ArgumentOutOfRangeException(sample.TubePosition.ToString(), "Request import: Invalid sample tray position");
        }

        return true;
    }

    private static int GetSampleTrayPosition(string tubePosition)
    {
        var trayPosition = 0;

        if (!string.IsNullOrEmpty(tubePosition) && tubePosition.Length is 2 or 3)
        {
            if (tubePosition[0] >= 'A' && tubePosition[0] <= 'E')
            {
                if (int.TryParse(tubePosition.AsSpan(1), out var column) && column >= 1 && column <= 10)
                {
                    var row = tubePosition[0] - 'A' + 1;

                    trayPosition = ((row - 1) * 10) + column;
                }
            }
        }

        return trayPosition;
    }

    private List<TubeType> GetTubeTypes()
    {
        return tubeSettings.Value.TubeTypeList.OrderBy(t => t.TubeDefinitionId).ToList();
    }

    private async Task<bool> ValidateImportedTest(ImportedSample sample, ImportedTest test, List<string> usedCustomTestIds)
    {
        var methodTest = await GetMethodTest(test.MethodName, test.TestName);
        if (methodTest == null)
        {
            throw new ArgumentOutOfRangeException(test.MethodName + "/" + test.TestName, "Request import: Unknown test");
        }

        if (test.Replicates != null && (test.Replicates.Value < 1 || test.Replicates.Value > 20))
        {
            throw new ArgumentOutOfRangeException(test.Replicates.Value.ToString(), "Request import: Illegal number of replicates");
        }

        if (test.InstrumentDilutionFactor != null && (test.InstrumentDilutionFactor.Value < 1 || test.InstrumentDilutionFactor.Value > 1000))
        {
            throw new ArgumentOutOfRangeException(test.InstrumentDilutionFactor.Value.ToString(), "Request import: Illegal Instrument Dilution Factor");
        }

        var (preventDilutionFactorsBelowDefault, defaultDilutionFactor, technology) = await methodService.GetDefaultInstrumentDilutionFactor(test.TestName, test.MethodName);

        var validInstrumentDilutionFactors = GetValidDilutionFactors(technology);

        if (test.InstrumentDilutionFactor != null && !validInstrumentDilutionFactors.Contains(test.InstrumentDilutionFactor.Value))
        {
            throw new ArgumentOutOfRangeException(test.InstrumentDilutionFactor.Value.ToString(), "Request import: Illegal Instrument Dilution Factor");
        }

        // Check dilution factor combinations...
        if (sample.PreDilutionFactor.HasValue && test.InstrumentDilutionFactor.HasValue)
        {
            if (preventDilutionFactorsBelowDefault && sample.PreDilutionFactor.Value * test.InstrumentDilutionFactor.Value < defaultDilutionFactor)
            {
                throw new ArgumentOutOfRangeException(test.MethodName + "/" + test.TestName, "Request import: Too low total dilution factors");
            }
        }

        if (!string.IsNullOrEmpty(test.CustomTestId))
        {
            if (usedCustomTestIds.Contains(test.CustomTestId))
            {
                throw new ArgumentOutOfRangeException(test.MethodName + "/" + test.TestName + "/" + test.CustomTestId, "Request import: Non-unique Custom Test Id");
            }
            else
            {
                usedCustomTestIds.Add(test.CustomTestId);
            }
        }

        // Check lot numbers
        CheckLotNumber(test, test.CalibratorCarrierLot);
        CheckLotNumber(test, test.CalibratorCartridgeLot);
        CheckLotNumber(test, test.CalibratorConjugateLot);
        CheckLotNumber(test, test.CarrierLot);
        CheckLotNumber(test, test.ConjugateLot);
        CheckLotNumber(test, test.DevelopmentLot);
        CheckLotNumber(test, test.DiluentLot);

        return true;
    }

    private static List<int> GetValidDilutionFactors(AssayTechnology? technology)
    {
        if (!technology.HasValue)
        {
            return [];
        }

        return technology switch
        {
            AssayTechnology.ImmunoCap => InstrumentDilutionFactors.ImmunoCap,
            AssayTechnology.EliA => InstrumentDilutionFactors.Elia,
            AssayTechnology.ImmunoCapIgG => InstrumentDilutionFactors.ImmunoCapIgG,
            AssayTechnology.ViewAllergy => InstrumentDilutionFactors.ViewAllergy,
            _ => throw new ArgumentOutOfRangeException(nameof(technology), technology, null)
        };
    }

    private static void CheckLotNumber(ImportedTest test, string lotNumber)
    {
        if (!string.IsNullOrEmpty(lotNumber) && lotNumber.Length > Constants.LotNumberLength)
        {
            throw new ArgumentOutOfRangeException(test.MethodName + "/" + test.TestName, "Request import: Too long lot number: " + lotNumber);
        }
    }

    private async Task SaveRequests(ImportedRequests requests, CancellationToken cancellationToken)
    {
        foreach (var importedSample in requests.Samples)
        {
            await SaveSample(requests.CustomRunId, importedSample, cancellationToken);
        }
    }

    private async Task SaveSample(string? customRunId, ImportedSample importedSample, CancellationToken cancellationToken)
    {
        var requestResponse = await mediator.Send(new GetRequestIdQuery(importedSample.SampleId), cancellationToken);

        Domain.Request request;

        if (requestResponse.RequestExists)
        {
            request = await dbContext.Requests.Include(s => s.TestOrders).ThenInclude(t => t.TestOrderCustomParameters).FirstOrDefaultAsync(s => s.Id == requestResponse.RequestId, cancellationToken) ?? throw new NotFoundException();
        }
        else
        {
            var tubeDefinition = GetTubeType(importedSample.TubeType);

            request = new Domain.Request
            {
                SampleId = importedSample.SampleId,
                PreDilutionFactor = importedSample.PreDilutionFactor != null ? importedSample.PreDilutionFactor.Value : 1,
                RequestId = !string.IsNullOrEmpty(importedSample.RequestId) ? importedSample.RequestId : null,
                SampleCollectionDate = importedSample.SampleDate != null ? importedSample.SampleDate.Value : dateTime.Now,
                RequestDate = dateTime.Now,
                SampleTubeType = tubeDefinition!.TubeDefinitionId,
                Source = RequestSource.Import,
                RequestorId = await GetRequestorId(importedSample.RequestorId, cancellationToken),
                TestOrders = new List<TestOrder>(),
                SampleVolumeCheck = SampleVolumeCheck.Off
            };

            // Check with sample module if any tubes are marked as high priority
            var markedAsHighPriority = await mediator.Send(new HasSampleTubeHighPriorityQuery(new RequestUid { SampleId = request.SampleId }), cancellationToken);
            if (markedAsHighPriority)
            {
                request.HighPriority = markedAsHighPriority;
            }

            await dbContext.Requests.AddAsync(request, cancellationToken);
        }

        foreach (var importedTest in importedSample.Tests)
        {
            await SaveTestOrder(customRunId, request, importedTest);
            request.UpdateRequestState();
        }
    }

    private TubeType? GetTubeType(int? tubeType)
    {
        var tubeTypes = GetTubeTypes();

        if (tubeType == null)
        {
            if (int.TryParse(tubeSettings.Value.NormalTubeDefinitionId, out var normalTubeId))
            {
                return tubeTypes.FirstOrDefault(t => t.TubeDefinitionId == normalTubeId);
            }

            throw new NotFoundException("NormalTubeType Not found");
        }

        return tubeTypes.FirstOrDefault(t => t.TubeDefinitionId == tubeType.Value);
    }

    private async Task SaveTestOrder(string? customRunId, Domain.Request request, ImportedTest importedTest)
    {
        if (ImportedTestIsUnique(request, importedTest))
        {
            var methodTest = await GetMethodTest(importedTest.MethodName, importedTest.TestName);
            if (methodTest != null)
            {
                var newTest = new TestOrder
                {
                    InstrumentDilutionFactor = await GetInstrumentDilutionFactor(importedTest.InstrumentDilutionFactor, importedTest.MethodName, importedTest.TestName),
                    LabTestName = await displayNameProvider.GetTestDisplayName(importedTest.MethodName, importedTest.TestName),
                    MethodName = importedTest.MethodName,
                    NumberOfReplicates = GetNumberOfReplicates(importedTest.Replicates, methodTest),
                    TestName = importedTest.TestName,
                    TestSortOrder = request.TestOrders!.Count + 1,
                    Uid = Guid.NewGuid()
                };
                SaveTestOrderCustomParameters(customRunId, newTest, importedTest);

                if (methodTest.DilutionMethod == DilutionMethod.PreDiluted)
                {
                    if (request.TestOrders.Count == 0)
                    {
                        request.PreDilutionFactor = methodTest.DilutionFactor;
                    }
                    else if (request.PreDilutionFactor != methodTest.DilutionFactor)
                    {
                        logger.LogError("Default pre-dilution factor for test {Test} is different than the requests pre-dilution factor for sample {SampleId}", importedTest.TestName, request.SampleId);
                    }
                }

                request.TestOrders!.Add(newTest);
            }
        }
    }

    private static bool ImportedTestIsUnique(Domain.Request request, ImportedTest importedTest)
    {
        var tests = request.TestOrders.Where(t => t.MethodName == importedTest.MethodName && t.TestName == importedTest.TestName).ToList();

        // No tests
        if (tests.Count == 0)
        {
            return true;
        }

        // These tests may prevent the imported test to be added (but...)
        var testsWithSpecificStatus = tests.Where(t => t.State is
            (TestOrderState.NotStarted or TestOrderState.Scheduled or
            TestOrderState.UnableToStart or TestOrderState.Started or
            TestOrderState.Pipetted)).ToList();

        // Check each test to see if it is the "same" as the imported test or not
        foreach (var test in testsWithSpecificStatus)
        {
            if (IsSameAsTestToImport(test, importedTest))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSameAsTestToImport(TestOrder test, ImportedTest importedTest)
    {
        var importedTestHasCustomParameter = ImportedTestHasCustomParameter(importedTest);
        var testHasCustomParameter = TestHasCustomParameter(test);

        return importedTestHasCustomParameter == testHasCustomParameter &&
               HaveSameCustomParameters(test, importedTest);
    }

    private static bool ImportedTestHasCustomParameter(ImportedTest importedTest)
    {
        return !string.IsNullOrEmpty(importedTest.CarrierLot) || !string.IsNullOrEmpty(importedTest.ConjugateLot) ||
               !string.IsNullOrEmpty(importedTest.DevelopmentLot) || !string.IsNullOrEmpty(importedTest.DiluentLot);
    }

    private static bool TestHasCustomParameter(TestOrder test)
    {
        if (test.TestOrderCustomParameters == null)
        {
            return false;
        }

        return !string.IsNullOrEmpty(test.TestOrderCustomParameters.CarrierLot) || !string.IsNullOrEmpty(test.TestOrderCustomParameters.ConjugateLot) ||
               !string.IsNullOrEmpty(test.TestOrderCustomParameters.DevelopmentLot) || !string.IsNullOrEmpty(test.TestOrderCustomParameters.DiluentLot);
    }

    private static bool HaveSameCustomParameters(TestOrder test, ImportedTest importedTest)
    {
        return test.TestOrderCustomParameters!.CarrierLot == importedTest.CarrierLot &&
               test.TestOrderCustomParameters!.ConjugateLot == importedTest.ConjugateLot &&
               test.TestOrderCustomParameters!.DevelopmentLot == importedTest.DevelopmentLot &&
               test.TestOrderCustomParameters!.DiluentLot == importedTest.DiluentLot;
    }

    private void SaveTestOrderCustomParameters(string? customRunId, TestOrder newTest, ImportedTest importedTest)
    {
        var customParameters = new TestOrderCustomParameter
        {
            CarrierLot = importedTest.CarrierLot,
            ConjugateLot = importedTest.ConjugateLot,
            DevelopmentLot = importedTest.DevelopmentLot,
            DiluentLot = importedTest.DiluentLot,
            CustomRunId = customRunId ?? string.Empty,
            CustomTestId = importedTest.CustomTestId,
            TestOrder = newTest
        };

        if (!string.IsNullOrEmpty(importedTest.CarrierLot) || !string.IsNullOrEmpty(importedTest.ConjugateLot) ||
            !string.IsNullOrEmpty(importedTest.DevelopmentLot) || !string.IsNullOrEmpty(importedTest.DiluentLot) ||
            !string.IsNullOrEmpty(customRunId) || !string.IsNullOrEmpty(importedTest.CustomTestId))
        {
            dbContext.TestOrderCustomParameters.Add(customParameters);
        }
    }

    private async Task<MethodTest?> GetMethodTest(string methodName, string testName)
    {
        return await methodService.GetMethodTest(methodName, testName, mt => mt);
    }

    private static int GetNumberOfReplicates(int? replicates, MethodTest methodTest)
    {
        if (replicates.HasValue)
        {
            return replicates.Value;
        }
        else
        {
            return methodTest.Replicates;
        }
    }

    private async Task<int> GetInstrumentDilutionFactor(int? importedInstrumentDilutionFactor, string methodName, string testName)
    {
        // TODO: correct dilution factor handling...
        if (importedInstrumentDilutionFactor.HasValue)
        {
            return importedInstrumentDilutionFactor.Value;
        }
        else
        {
            var (_, defaultDilutionFactor, _) = await methodService.GetDefaultInstrumentDilutionFactor(testName, methodName);

            return defaultDilutionFactor;
        }
    }

    private async Task<int?> GetRequestorId(string importedRequestorId, CancellationToken cancellationToken)
    {
        int? requestorId = await dbContext.Requestors.Where(r => r.RequestorId == importedRequestorId).Select(r => r.Id).FirstOrDefaultAsync(cancellationToken);
        if (requestorId == 0)
        {
            requestorId = null;
        }

        return requestorId;
    }
}
