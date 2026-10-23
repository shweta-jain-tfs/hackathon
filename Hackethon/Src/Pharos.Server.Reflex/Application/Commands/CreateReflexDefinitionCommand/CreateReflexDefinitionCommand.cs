using System.Text;
using FluentValidation;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Parameters;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Localization;
using Pharos.Server.Reflex.Application.Events;
using Pharos.Server.Reflex.Application.Mapping;
using Pharos.Server.Reflex.Application.Queries;
using Pharos.Server.Reflex.Domain;
using Pharos.Server.Reflex.Interfaces;

namespace Pharos.Server.Reflex.Application.Commands.CreateReflexDefinitionCommand;

public class CreateReflexDefinitionCommand : IRequest<int>
{
    public string Name { get; set; } = "";
    public bool Automatic { get; set; }
    public bool Active { get; set; }
    public bool HasRequestorEnabled { get; set; }
    public string? RequestorId { get; set; }
    public string? CommentCode { get; set; }
    public List<ReflexTestDto>? ReflexTests { get; set; }
    public ReflexConditionDto? ReflexCondition { get; set; }
}

internal sealed class CreateReflexDefinitionCommandHandler(IReflexDbContext dbContext, IMethodsService methods, IOptionsSnapshot<RequestSettings> requestSettings, IAuthorizationService authorizationService) : IRequestHandler<CreateReflexDefinitionCommand, int>
{
    private static readonly CompositeFormat _reflexExists = CompositeFormat.Parse(Translations.ReflexAlreadyExists);

    public async Task<int> Handle(CreateReflexDefinitionCommand reflexDefinitionCommand, CancellationToken cancellationToken)
    {
        var personalAndHealthPermissions = await authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);

        var canHandleRequestorInfo =
            requestSettings.Value.IsRequestorInformationEnabled &&
            personalAndHealthPermissions;

        if (dbContext.ReflexDefinitions.Any(s => s.Name == reflexDefinitionCommand.Name))
        {
            var message = string.Format(null, _reflexExists, reflexDefinitionCommand.Name);
            throw new ValidationException(message, [new(nameof(reflexDefinitionCommand.Name), message)]);
        }

        foreach (var reflexTest in reflexDefinitionCommand.ReflexTests ?? Enumerable.Empty<ReflexTestDto>())
        {
            await ValidateReflexTest(reflexTest).ConfigureAwait(false);
        }

        var reflexEntity = CreateReflexDefinition(reflexDefinitionCommand, canHandleRequestorInfo);

        HandleReflexConditions(reflexEntity, reflexDefinitionCommand.ReflexCondition);

        HandleReflexTests(reflexEntity, reflexDefinitionCommand.ReflexTests);

        reflexEntity.AddDomainEvent(new ReflexDefinitionCreatedEvent(reflexEntity));

        dbContext.ReflexDefinitions.Add(reflexEntity);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return reflexEntity.Id;
    }

    private async Task ValidateReflexTest(ReflexTestDto reflexTest)
    {
        // Project to a named DTO instead of an anonymous type so it can be easily mocked in tests.
        var test = await methods.GetMethodTest(reflexTest.MethodName, reflexTest.TestName, x => new ReflexMethodTestValidationInfo
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

    private static ReflexDefinition CreateReflexDefinition(CreateReflexDefinitionCommand reflex, bool canHandleRequestorInfo)
    {
        var reflexDefinition = new ReflexDefinition
        {
            Name = reflex.Name,
            Automatic = reflex.Automatic,
            Active = reflex.Active,
            RequestorId = null
        };

        if (canHandleRequestorInfo && reflex.HasRequestorEnabled && !string.IsNullOrWhiteSpace(reflex.RequestorId))
        {
            reflexDefinition.RequestorId = reflex.RequestorId;
        }

        return reflexDefinition;
    }

    private static void HandleReflexConditions(ReflexDefinition reflex, ReflexConditionDto? condition)
    {
        if (condition is not null)
        {
            reflex.ReflexCondition = condition.ToReflexCondition();
        }
    }

    private static void HandleReflexTests(ReflexDefinition reflex, List<ReflexTestDto>? tests)
    {
        if (tests is not null)
        {
            foreach (var test in tests)
            {
                if (tests.Where(x => x != test).Any(x => x.MethodName == test.MethodName && x.TestName == test.TestName))
                {
                    throw new ValidationException(Translations.ReflexTestDuplicate);
                }

                reflex.ReflexTests.Add(new ReflexTest
                {
                    TestName = test.TestName,
                    MethodName = test.MethodName,
                    NumberOfReplicates = test.NumberOfReplicates,
                    InstrumentDilutionFactor = test.InstrumentDilutionFactor,
                    TestSortOrder = test.TestSortOrder,
                });
            }
        }
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