using System.Text;
using FluentValidation;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Parameters;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Localization;
using Pharos.Server.Reflex.Application.Events;
using Pharos.Server.Reflex.Domain;
using Pharos.Server.Reflex.Interfaces;
using Pharos.Server.Application.Security;

namespace Pharos.Server.Reflex.Application.Commands.UpdateReflexDefinitionCommand;

public class UpdateReflexDefinitionCommand : IRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Automatic { get; set; }
    public bool Active { get; set; }
    public bool HasRequestorEnabled { get; set; }
    public string? RequestorId { get; set; } = "";
    public string? CommentCode { get; set; } = "";
    public UpdatedReflexCondition? ReflexCondition { get; set; }
    public DateTimeOffset ReflexDefinitionQueriedAt { get; set; }
    public List<UpdatedReflexTest>? ReflexTests { get; set; }
}

internal sealed class UpdateReflexDefinitionCommandHandler : IRequestHandler<UpdateReflexDefinitionCommand>
{
    private readonly IReflexDbContext _dbReflexContext;
    private readonly IMethodsService _methods;
    private static readonly CompositeFormat _reflexExists = CompositeFormat.Parse(Translations.ReflexAlreadyExists);
    private readonly IOptionsSnapshot<RequestSettings> _requestSettings;
    private readonly IAuthorizationService _authorizationService;

    public UpdateReflexDefinitionCommandHandler(IReflexDbContext dbReflexContext, IMethodsService methods, IOptionsSnapshot<RequestSettings> requestSettings, IAuthorizationService authorizationService)
    {
        _dbReflexContext = dbReflexContext;
        _methods = methods;
        _requestSettings = requestSettings;
        _authorizationService = authorizationService;
    }

    public async Task Handle(UpdateReflexDefinitionCommand reflexCommand, CancellationToken cancellationToken)
    {
        var personalAndHealthPermissions = await _authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);

        var canHandleRequestorInfo =
            _requestSettings.Value.IsRequestorInformationEnabled &&
            personalAndHealthPermissions;

        var reflexEntity = await _dbReflexContext.ReflexDefinitions
            .Where(r => r.Id == reflexCommand.Id)
            .Include(r => r.ReflexCondition)
            .Include(s => s.ReflexTests)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (reflexEntity == null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }

        foreach (var reflexTest in reflexCommand.ReflexTests ?? Enumerable.Empty<UpdatedReflexTest>())
        {
            await ValidateReflexTest(reflexTest).ConfigureAwait(false);
        }

        if (_dbReflexContext.ReflexDefinitions.Any(s => s.Name == reflexCommand.Name && s.Id != reflexCommand.Id))
        {
            var message = string.Format(null, _reflexExists, reflexCommand.Name);
            throw new ValidationException(message, [new(nameof(reflexCommand.Name), message)]);
        }

        UpdateReflexDefinitionProperties(reflexEntity, reflexCommand, canHandleRequestorInfo);

        AssignReflexConditionProperties(reflexEntity.ReflexCondition, reflexCommand.ReflexCondition!);

        HandleReflexTests(reflexEntity, reflexCommand);

        reflexEntity.AddDomainEvent(new ReflexDefinitionUpdatedEvent(reflexEntity));
        await _dbReflexContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ValidateReflexTest(UpdatedReflexTest reflexTest)
    {
        // Project to a named DTO instead of an anonymous type so it can be easily mocked in tests.
        var test = await _methods.GetMethodTest(reflexTest.MethodName, reflexTest.TestName, x => new ReflexMethodTestValidationInfo
        {
            Technology = x.Method.Technology,
            InstrumentDilutionFactor = x.InstrumentDilutionFactor
        });

        if (test == null)
        {
            // Do not show the invalid test names to the user, because Qualys complains about it (as vulnerability).
            throw new ValidationException(Translations.InvalidTestName);
        }

        if ((test.Technology == AssayTechnology.ImmunoCap && !InstrumentDilutionFactors.ImmunoCap.Contains(test.InstrumentDilutionFactor))
            || (test.Technology == AssayTechnology.EliA && !InstrumentDilutionFactors.Elia.Contains(test.InstrumentDilutionFactor))
            || (test.Technology == AssayTechnology.ImmunoCapIgG && !InstrumentDilutionFactors.ImmunoCapIgG.Contains(test.InstrumentDilutionFactor))
            || (test.Technology == AssayTechnology.ViewAllergy && !InstrumentDilutionFactors.ViewAllergy.Contains(test.InstrumentDilutionFactor)))
        {
            throw new ValidationException(Translations.InvalidInstrumentDilutionFactor);
        }
    }

    private static void UpdateReflexDefinitionProperties(ReflexDefinition reflex, UpdateReflexDefinitionCommand command, bool canHandleRequestorInfo)
    {
        reflex.Name = command.Name;
        reflex.Automatic = command.Automatic;
        reflex.Active = command.Active;

        if (canHandleRequestorInfo)
        {
            if (command.HasRequestorEnabled && !string.IsNullOrWhiteSpace(command.RequestorId))
            {
                reflex.RequestorId = command.RequestorId;
            }
            else
            {
                reflex.RequestorId = null;
            }
        }
    }

    private void HandleReflexTests(ReflexDefinition reflex, UpdateReflexDefinitionCommand reflexCommand)
    {
        var updatedTests = reflexCommand.ReflexTests;
        if (updatedTests is null)
        {
            return;
        }

        var existingReflexTests = reflex.ReflexTests.ToList();

        var deletedReflexTests = existingReflexTests
            .Where(x => x.Created < reflexCommand.ReflexDefinitionQueriedAt && !updatedTests.Exists(y => y.Id == x.Id))
            .ToList();

        var testSortOrder = 1;
        foreach (var order in updatedTests.OrderBy(x => x.TestSortOrder))
        {
            if (order.Id.HasValue) // update
            {
                // update existing reflex test
                var existingReflexTest = existingReflexTests.FirstOrDefault(t => t.Id == order.Id)
                                         ?? throw new ConflictException(Translations.ConflictingEdits); // the reflex test has been deleted by another user

                AssignTestOrderProperties(existingReflexTest, order);

                existingReflexTest.TestSortOrder = testSortOrder;
            }
            else // new
            {
                // Validation for adding duplicate reflex tests (added the same test twice)
                if (updatedTests.Any(x => x != order && !x.Id.HasValue && x.TestName == order.TestName && x.MethodName == order.MethodName))
                {
                    throw new ValidationException(Translations.ReflexTestDuplicate);
                }

                // add new reflex test
                var newReflexTest = new ReflexTest();
                AssignTestOrderProperties(newReflexTest, order);
                newReflexTest.TestSortOrder = testSortOrder;
                reflex.ReflexTests.Add(newReflexTest);
            }

            testSortOrder++;
        }

        // remove the deleted reflex tests
        foreach (var deletedReflexTest in deletedReflexTests)
        {
            _dbReflexContext.ReflexTests.Remove(deletedReflexTest);
            reflex.ReflexTests.Remove(deletedReflexTest);
        }
    }

    private static void AssignTestOrderProperties(ReflexTest reflexTest, UpdatedReflexTest updatedTest)
    {
        reflexTest.TestName = updatedTest.TestName;
        reflexTest.MethodName = updatedTest.MethodName;
        reflexTest.InstrumentDilutionFactor = updatedTest.InstrumentDilutionFactor;
        reflexTest.NumberOfReplicates = updatedTest.NumberOfReplicates;
        reflexTest.TestSortOrder = updatedTest.TestSortOrder;
    }
    
    private static void AssignReflexConditionProperties(ReflexCondition existingReflexCondition, UpdatedReflexCondition condition)
    {
        existingReflexCondition.TestName = condition.TestName ?? string.Empty;
        existingReflexCondition.MethodName = condition.MethodName ?? string.Empty;
        existingReflexCondition.CompareType = condition.CompareType;
        existingReflexCondition.ResultType = condition.ResultType;
        existingReflexCondition.ConcentrationCompareValue = condition.ConcentrationCompareValue;
        existingReflexCondition.ConcentrationCompareValueUpperLimit = condition.ConcentrationCompareValueUpperLimit;
        existingReflexCondition.ConcentrationRawCompareValue = condition.ConcentrationRawCompareValue;
        existingReflexCondition.ConcentrationRawCompareValueUpperLimit = condition.ConcentrationRawCompareValueUpperLimit;
        existingReflexCondition.ClassCompareValue = condition.ClassCompareValue;
        existingReflexCondition.QuotientCompareValue = condition.QuotientCompareValue;
        existingReflexCondition.QuotientCompareValueUpperLimit = condition.QuotientCompareValueUpperLimit;
        existingReflexCondition.CutOffCompareValue = condition.CutOffCompareValue;
        existingReflexCondition.RuCompareValue = condition.RuCompareValue;
    }

    /// <summary>
    /// Lightweight DTO used when validating reflex tests against method parameters.
    /// Exists to avoid using anonymous types with the generic IMethodsService API so it can be mocked in tests.
    /// </summary>
    internal sealed class ReflexMethodTestValidationInfo
    {
        public AssayTechnology Technology { get; set; }

        public int InstrumentDilutionFactor { get; set; }
    }
}
