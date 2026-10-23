using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Parameters;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Request.Application.Helpers;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Mapping;
using Pharos.Server.Request.Application.Queries.GetPatient;
using Pharos.Server.Request.Application.Queries.GetRequestor;
using Pharos.Server.Localization;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Commands.CreateUpdateRequest;

public abstract class BaseUpdateRequestCommand : IRequest<int>
{
    public bool HighPriority { get; set; }
    public PatientDto? Patient { get; set; }
    public int PreDilutionFactor { get; set; }
    public DateTimeOffset? RequestDate { get; set; }
    public string? RequestId { get; set; }
    public RequestorDto? Requestor { get; set; }
    public int? SampleTubeType { get; set; }
    public DateTimeOffset? SampleCollectionDate { get; set; }
    public SampleVolumeCheck SampleVolumeCheck { get; set; }
    public List<NewUpdatedTest>? TestOrders { get; set; }
}

internal abstract class BaseUpdateRequestCommandHandler<TBaseUpdateRequestCommand> : IRequestHandler<TBaseUpdateRequestCommand, int>
    where TBaseUpdateRequestCommand : BaseUpdateRequestCommand
{
    private readonly IAuthorizationService _authorizationService;
    private readonly RequestSettings _requestSettings;
    private readonly TubeTypesSettings _tubeSettings;
    private readonly IMethodsService _methods;

    protected BaseUpdateRequestCommandHandler(
        IAuthorizationService authorizationService,
        IRequestDbContext dbContext,
        IOptionsSnapshot<RequestSettings> requestSettings,
        IOptionsSnapshot<TubeTypesSettings> tubeSettings,
        IMediator mediator,
        IMethodsService methods)
    {
        _authorizationService = authorizationService;
        DbContext = dbContext;
        _requestSettings = requestSettings.Value;
        _tubeSettings = tubeSettings.Value;
        Mediator = mediator;
        _methods = methods;
    }

    public virtual async Task<int> Handle(TBaseUpdateRequestCommand command, CancellationToken cancellationToken)
    {
        var personalAndHealthPermissions = await _authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);

        CanUpdatePatient = _requestSettings.IsPatientInformationEnabled && personalAndHealthPermissions;

        CanUpdateRequestor =
            _requestSettings.IsRequestorInformationEnabled && personalAndHealthPermissions;

        var request = await GetRequest(command, cancellationToken); // CanUpdatePatient and CanUpdateRequestor are used in override

        var preDilutionChanged = ValidatePreDilutionAndSampleTubeType(request, command); // We need to validate pre-dilution before tests
        
        if (command.TestOrders is { Count: > 0 })
        {
            await ValidateTests(request, command, preDilutionChanged);
            await ValidateTestPanels(command, cancellationToken);
        }

        UpdateRequestProperties(request, command);

        if (CanUpdatePatient)
        {
            await SetPatientAge(request, command.Patient?.PatientId, command.Patient?.BirthDate, cancellationToken);
            await AddOrUpdatePatient(request, command.Patient, cancellationToken);
        }

        if (CanUpdateRequestor)
        {
            await AddOrUpdateRequestor(request, command.Requestor, cancellationToken);
        }

        await UpdateTestOrders(request, command, cancellationToken);

        await DbContext.SaveChangesAsync(cancellationToken);

        await PublishNotifications(request);

        return request.Id;
    }

    protected bool CanUpdatePatient { get; private set; }

    protected bool CanUpdateRequestor { get; private set; }

    protected bool RequestPropertiesChanged { get; set; }

    protected bool TubePropertiesChanged { get; private set; }

    protected IRequestDbContext DbContext { get; }

    protected IMediator Mediator { get; }

    protected abstract Task<Domain.Request> GetRequest(TBaseUpdateRequestCommand command, CancellationToken cancellationToken);

    protected abstract Task UpdateTestOrders(Domain.Request request, TBaseUpdateRequestCommand command, CancellationToken cancellationToken);

    protected abstract Task PublishNotifications(Domain.Request request);

    protected virtual bool ValidatePreDilutionAndSampleTubeType(Domain.Request request, TBaseUpdateRequestCommand command)
    {
        if (command.PreDilutionFactor == 0)
        {
            command.PreDilutionFactor = 1; // Set default pre-dilution factor if not provided
        }

        if (command.SampleTubeType == null)
        {
            throw new ValidationException("Sample tube type must be set.");
        }

        if (_tubeSettings.TubeTypeList.FirstOrDefault(t => t.TubeDefinitionId == command.SampleTubeType) == null)
        {
            throw new ValidationException($"Invalid sample tube type ID: {command.SampleTubeType}");
        }

        return command.PreDilutionFactor != request.PreDilutionFactor;
    }

    protected virtual async Task ValidateTests(Domain.Request request, TBaseUpdateRequestCommand command, bool preDilutionChanged)
    {
        var tests = command.TestOrders!;

        // We do not check existing tests because changing method or test name for existing test is not allowed.
        var methodNames = tests
            .Where(t => t.Id == 0)
            .Select(t => t.MethodName)
            .ToHashSet();

        var testNames = tests
            .Where(t => t.Id == 0)
            .Select(t => t.TestName)
            .ToHashSet();

        // Also include method and test names from existing tests, even though changing them is not allowed.
        // We need to get method information for all existing tests to validate dilution.
        foreach (var test in request.TestOrders)
        {
            if (!string.IsNullOrEmpty(test.MethodName))
            {
                methodNames.Add(test.MethodName);
            }

            if (!string.IsNullOrEmpty(test.TestName))
            {
                testNames.Add(test.TestName);
            }
        }

        var methods = await _methods.GetMethods(
            x => methodNames.Contains(x.MethodName),
            x => new
            {
                x.MethodName,
                x.Technology,
                x.PreventDilutionFactorsBelowDefault,
                Tests = x.Tests
                    .Where(t => t.Active && testNames.Contains(t.TestName))
                    .Select(t => new
                    {
                        Name = t.TestName,
                        t.DilutionMethod,
                        t.DilutionFactor,
                        LabNames = t.LabTestNames.Select(n => n.TestName).ToList()
                    })
            });

        if (methodNames.Count > 0 && (methods == null || methods.Count == 0 || methods.Count != methodNames.Count))
        {
            // Do not show the invalid method names to the user, because Qualys complains about it (as vulnerability).
            throw new ValidationException("Invalid method name specified. Check that all needed methods are enabled in parameters.");
        }

        foreach (var test in tests)
        {
            string methodName, testName;
            if (test.Id > 0)
            {
                var t = request.TestOrders.First(to => to.Id == test.Id); // the test exists, otherwise the overriden method would throw a validation exception
                if (!preDilutionChanged && (test.InstrumentDilutionFactor == 0 || test.InstrumentDilutionFactor == t.InstrumentDilutionFactor))
                {
                    // the dilution factor has not been changed, we can skip validation for this test because
                    // changing method or test name for existing test is not allowed, so method and test names are valid.
                    continue;
                }

                methodName = t.MethodName;
                testName = t.TestName;
            }
            else
            {
                methodName = test.MethodName;
                testName = test.TestName;

                // Check for test duplicates among the added tests,
                // additional check on request is done later, after deleting tests (see override).
                if (tests.Any(x => x != test && x.Id == 0 && x.MethodName == test.MethodName && x.TestName == test.TestName))
                {
                    throw new ValidationException(Translations.TestOrderDuplicate);
                }
            }

            var method = methods.First(m => m.MethodName == methodName);
            var methodTest = method.Tests.FirstOrDefault(t => t.Name == testName);
            if (methodTest == null)
            {
                // Do not show the invalid test names to the user, because Qualys complains about it (as vulnerability).
                throw new ValidationException("Invalid test name specified. Check that all needed tests are enabled in parameters.");
            }

            if (!string.IsNullOrEmpty(test.LabTestName))
            {
                if (methodTest.LabNames.All(n => n != test.LabTestName))
                {
                    throw new ValidationException("Invalid laboratory test name specified.");
                }
            }

            if ((method.Technology == AssayTechnology.ImmunoCap && !InstrumentDilutionFactors.ImmunoCap.Contains(test.InstrumentDilutionFactor))
                || (method.Technology == AssayTechnology.EliA && !InstrumentDilutionFactors.Elia.Contains(test.InstrumentDilutionFactor))
                || (method.Technology == AssayTechnology.ImmunoCapIgG && !InstrumentDilutionFactors.ImmunoCapIgG.Contains(test.InstrumentDilutionFactor))
                || (method.Technology == AssayTechnology.ViewAllergy && !InstrumentDilutionFactors.ViewAllergy.Contains(test.InstrumentDilutionFactor)))
            {
                throw new ValidationException(Translations.InvalidInstrumentDilutionFactor);
            }

            var defaultDilution = methodTest.DilutionMethod == DilutionMethod.PreDiluted ? 1 : methodTest.DilutionFactor;

            if (method.PreventDilutionFactorsBelowDefault && test.InstrumentDilutionFactor * command.PreDilutionFactor < defaultDilution)
            {
                throw new ValidationException(Translations.CombinedDilutionFactorBelowMinimum);
            }
        }
    }

    private async Task AddOrUpdatePatient(Domain.Request request, PatientDto? patientDto, CancellationToken cancellationToken)
    {
        var originalPatientId = request.Patient?.Id;

        if (patientDto is null || string.IsNullOrEmpty(patientDto.PatientId))
        {
            request.Patient = null;

            // Check if the original patient might have become an orphan
            await DeleteOrphanPatient(request.Id, originalPatientId, cancellationToken);

            RequestPropertiesChanged = true;

            return;
        }

        // Update current patient with same PID
        if (request.Patient != null && request.Patient.PatientId == patientDto.PatientId)
        {
            PatientMapper.UpdatePatientFromDto(patientDto, request.Patient);
            RequestPropertiesChanged = true;
        }
        // If the PID is not the same as the current patient
        else if (!string.IsNullOrEmpty(patientDto.PatientId))
        {
            var existingPatient = await DbContext.Patients.Where(x => x.PatientId == patientDto.PatientId).FirstOrDefaultAsync(cancellationToken);

            // If the patient exists it will be updated and attached to request
            if (existingPatient != null)
            {
                PatientMapper.UpdatePatientFromDto(patientDto, existingPatient);

                request.Patient = existingPatient;
            }
            // If the patient does not exist, a new patient will be created
            else
            {
                var patient = new Patient
                {
                    PatientId = patientDto.PatientId
                };
                PatientMapper.UpdatePatientFromDto(patientDto, patient);
                request.Patient = patient;
            }
            RequestPropertiesChanged = true;

            // Check if the original patient might have become an orphan
            await DeleteOrphanPatient(request.Id, originalPatientId, cancellationToken);
        }
    }

    private async Task DeleteOrphanPatient(int requestId, int? originalPatientId, CancellationToken cancellationToken)
    {
        if (!originalPatientId.HasValue)
        {
            return;
        }

        var patientWithRequests = await DbContext.Patients.Include(x => x.Requests).Where(x => x.Id == originalPatientId).FirstOrDefaultAsync(cancellationToken);

        // If no other requests are attached to the patient, delete it
        if (patientWithRequests?.Requests?.Count(x => x.Id != requestId) == 0)
        {
            DbContext.Patients.Remove(patientWithRequests);
        }
    }

    private async Task AddOrUpdateRequestor(Domain.Request request, RequestorDto? requestorDto, CancellationToken cancellationToken)
    {
        if (requestorDto == null || string.IsNullOrEmpty(requestorDto.RequestorId))
        {
            request.Requestor = null;
            return;
        }

        if (request.Requestor != null && request.Requestor.RequestorId == requestorDto.RequestorId)
        {
            // the requestor is the same as the current one
            return;
        }

        // another requestor
        if (!string.IsNullOrEmpty(requestorDto.RequestorId))
        {
            var existingRequestor = await DbContext.Requestors.Where(x => x.RequestorId == requestorDto.RequestorId).FirstOrDefaultAsync(cancellationToken);
            request.Requestor = existingRequestor;
            RequestPropertiesChanged = true;
        }
    }

    protected async Task<TestOrder> AddTestOrder(Domain.Request request, NewUpdatedTest newTest, int testSortOrder, CancellationToken cancellationToken)
    {
        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            State = TestOrderState.NotStarted,
            MethodName = newTest.MethodName,
            TestName = newTest.TestName,
            LabTestName = newTest.LabTestName,
            InstrumentDilutionFactor = newTest.InstrumentDilutionFactor,
            NumberOfReplicates = newTest.NumberOfReplicates,
            TestSortOrder = testSortOrder,
            TestPanelIds = newTest.TestPanelIds,
            IsActive = newTest.IsActive
        };

        // Adds reflex definitions to a test order based on the provided test details.
        var freshReflexDefinitions = await ReflexDefinitionHelper.CreateFreshReflexDefinitionsAsync(
            Mediator,
            testOrder.TestName,
            testOrder.MethodName,
            newTest.ReflexDefinitionIds ?? [],
            cancellationToken);
        testOrder.ReflexDefinitions = freshReflexDefinitions;

        request.TestOrders.Add(testOrder);
        return testOrder;
    }

    private async Task SetPatientAge(Domain.Request request, string? patientId, DateOnly? birthDate, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(patientId))
        {
            request.PatientAge = null;
            return;
        }

        // Calculate age based on sample collection date (or request date as fallback)
        request.PatientAge = GetAgeFromBirthDate(birthDate, request);

        // Check if birthdate has changed for existing patient - if so, recalculate age for all their requests
        var existingPatient = await DbContext.Patients
            .Include(x => x.Requests)
            .FirstOrDefaultAsync(p => p.PatientId == patientId, cancellationToken);

        if (existingPatient?.BirthDate != null &&
            existingPatient.BirthDate != birthDate &&
            existingPatient.Requests is { Count: > 0 })
        {
            // Birthdate has changed - recalculate age for all requests of this patient
            foreach (var existingRequest in existingPatient.Requests)
            {
                // Skip the current request if it's already in the list (for existing requests being updated)
                if (existingRequest.Id == request.Id)
                {
                    continue;
                }

                existingRequest.PatientAge = GetAgeFromBirthDate(birthDate, existingRequest);
            }
        }
    }

    private static int? GetAgeFromBirthDate(DateOnly? currentPatientBirthDate, Domain.Request curRequest)
    {
        int? age = null;

        if (currentPatientBirthDate is null)
        {
            return age;
        }

        // Use SampleCollectionDate for medical accuracy (patient's age when sample was taken),
        // fall back to RequestDate or Created if not available
        var ageReferenceDate = curRequest.SampleCollectionDate
                               ?? curRequest.RequestDate
                               ?? curRequest.Created;

        age = ageReferenceDate.Year - currentPatientBirthDate.Value.Year;

        if (ageReferenceDate.Month < currentPatientBirthDate.Value.Month ||
            (ageReferenceDate.Month == currentPatientBirthDate.Value.Month && ageReferenceDate.Day < currentPatientBirthDate.Value.Day))
        {
            age--;
        }

        if (age < 0)
        {
            age = 0;
        }

        return age;
    }

    private void UpdateRequestProperties(Domain.Request request, TBaseUpdateRequestCommand command)
    {
        RequestPropertiesChanged = UpdatePropertyIfChanged(request.SampleVolumeCheck, v => request.SampleVolumeCheck = v, command.SampleVolumeCheck);
        RequestPropertiesChanged |= UpdatePropertyIfChanged(request.SampleCollectionDate, v => request.SampleCollectionDate = v, command.SampleCollectionDate);
        RequestPropertiesChanged |= UpdatePropertyIfChanged(request.RequestId, v => request.RequestId = v, command.RequestId);
        RequestPropertiesChanged |= UpdatePropertyIfChanged(request.RequestDate, v => request.RequestDate = v, command.RequestDate);

        TubePropertiesChanged = UpdatePropertyIfChanged(request.PreDilutionFactor, v => request.PreDilutionFactor = v, command.PreDilutionFactor);
        TubePropertiesChanged |= UpdatePropertyIfChanged(request.HighPriority, v => request.HighPriority = v, command.HighPriority);
        TubePropertiesChanged |= UpdatePropertyIfChanged(request.SampleTubeType, v => request.SampleTubeType = v, command.SampleTubeType);

        RequestPropertiesChanged |= TubePropertiesChanged;
    }

    private static bool UpdatePropertyIfChanged<T>(T currentValue, Action<T> setValue, T newValue)
    {
        if (!EqualityComparer<T>.Default.Equals(currentValue, newValue))
        {
            setValue(newValue);
            return true;
        }
        return false;
    }

    private async Task ValidateTestPanels(TBaseUpdateRequestCommand command, CancellationToken cancellationToken)
    {
        // If any test order has a panel id. Validate that all tests belonging to a panel has all of its siblings
        var panelIdsUsed = command.TestOrders!
            .Where(x => x.TestPanelIds is { Count: > 0 })
            .SelectMany(x => x.TestPanelIds.Select(id => id)).Distinct().ToList();

        if (panelIdsUsed.Count > 0)
        {
            var panelDefinitions = await GetPanels(panelIdsUsed, cancellationToken);
            foreach (var panelDefinition in panelDefinitions)
            {
                foreach (var panelTest in panelDefinition.Tests)
                {
                    var testExists = command.TestOrders!.Any(x =>
                        x.MethodName == panelTest.MethodTest.Method.MethodName &&
                        x.TestName == panelTest.MethodTest.TestName);

                    if (!testExists)
                    {
                        throw new ValidationException(string.Format(null, CompositeFormat.Parse(Translations.NotAllTestsFromPanel), panelDefinition.PanelName, panelTest.MethodTest.TestName));
                    }

                    var testWithPanelId = command.TestOrders!.Any(x =>
                        x.MethodName == panelTest.MethodTest.Method.MethodName &&
                        x.TestName == panelTest.MethodTest.TestName &&
                        x.TestPanelIds.Contains(panelDefinition.Id));

                    if (!testWithPanelId)
                    {
                        throw new ValidationException(string.Format(null, CompositeFormat.Parse(Translations.MissingPanelIdForTest), panelTest.MethodTest.TestName, panelDefinition.Id));
                    }
                }
            }
        }
    }

    private async Task<List<Panel>> GetPanels(List<int> panelIds, CancellationToken cancellationToken)
    {
        var panels = new List<Panel>();
        foreach (var panelId in panelIds)
        {
            var panel = await Mediator.Send(new GetPanelByIdQuery(panelId), cancellationToken);
            if (panel is null)
            {
                throw new ValidationException(string.Format(null, CompositeFormat.Parse(Translations.PanelNotFound), panelId));
            }

            panels.Add(panel);
        }

        return panels;
    }
}
