using System.Text;
using FluentValidation;
using MediatR;
using Pharos.Contracts.Parameters;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Localization;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.Panels;

public class UpdatePanelCommand : IRequest
{
    public int Id { get; set; }
    public string PanelName { get; set; } = string.Empty;
    public string PanelLongName { get; set; } = string.Empty;
    public bool Grouped { get; set; }
    public bool Enabled { get; set; }
    public List<MethodPanelTestsDto> Tests { get; set; } = [];
}

internal sealed class UpdatePanelCommandHandler(IParametersDbContext context) : IRequestHandler<UpdatePanelCommand>
{
    private static readonly CompositeFormat _panelAlreadyExistsFormat = CompositeFormat.Parse(Translations.PanelAlreadyExists);

    public async Task Handle(UpdatePanelCommand request, CancellationToken cancellationToken)
    {
        var panel = await context.Panels
            .Include(x => x.Tests)
            .ThenInclude(x => x.MethodTest)
            .ThenInclude(x => x.Method)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (panel is null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }

        if (await context.Panels.AnyAsync(x => x.Id != request.Id && x.PanelName == request.PanelName, cancellationToken))
        {
            throw new ValidationException(string.Format(null, _panelAlreadyExistsFormat, request.PanelName));
        }

        ValidateDuplicateTests(request.Tests);

        panel.PanelName = request.PanelName;
        panel.PanelLongName = request.PanelLongName;
        panel.GroupedPanel = request.Grouped;
        panel.Enabled = request.Enabled;

        var incomingIds = request.Tests
            .Where(x => x.Id is > 0)
            .Select(x => x.Id)
            .ToHashSet();

        var deletedTests = panel.Tests
            .Where(x => !incomingIds.Contains(x.Id) && !request.Tests.Any(t => t.Id == 0 && t.MethodName == x.MethodTest.Method.MethodName && t.TestName == x.MethodTest.TestName))
            .ToList();

        foreach (var deletedTest in deletedTests)
        {
            context.PanelTests.Remove(deletedTest);
            panel.Tests.Remove(deletedTest);
        }

        var sortOrder = 1;
        foreach (var test in request.Tests.OrderBy(x => x.SortOrder))
        {
            var methodTest = await context.MethodTests
                .Include(x => x.Method)
                .FirstOrDefaultAsync(x => x.Method.MethodName == test.MethodName && x.TestName == test.TestName, cancellationToken);

            if (methodTest is null)
            {
                throw new ValidationException(Translations.InvalidTestName);
            }

            var panelTest = test.Id is > 0
                ? panel.Tests.FirstOrDefault(x => x.Id == test.Id)
                : panel.Tests.FirstOrDefault(x => x.MethodTest.Method.MethodName == test.MethodName && x.MethodTest.TestName == test.TestName);

            if (panelTest is null)
            {
                panelTest = new PanelTest { Panel = panel };
                panel.Tests.Add(panelTest);
            }

            ApplyPanelTestValues(panelTest, test, methodTest, sortOrder++);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateDuplicateTests(IEnumerable<MethodPanelTestsDto> tests)
    {
        var hasDuplicates = tests
            .GroupBy(x => new { x.MethodName, x.TestName })
            .Any(x => x.Count() > 1);

        if (hasDuplicates)
        {
            throw new ValidationException(Translations.PanelTestDuplicate);
        }
    }

    private static void ApplyPanelTestValues(PanelTest panelTest, MethodPanelTestsDto test, MethodTest methodTest, int sortOrder)
    {
        ValidateInstrumentDilutionFactor(test, methodTest);

        panelTest.MethodTest = methodTest;
        panelTest.MethodTestId = methodTest.Id;
        panelTest.Replicates = test.Replicates == 0 ? methodTest.Replicates : test.Replicates;
        panelTest.InstrumentDilutionFactor = ResolveInstrumentDilutionFactor(test, methodTest);
        panelTest.SortOrder = sortOrder;
    }

    private static int ResolveInstrumentDilutionFactor(MethodPanelTestsDto test, MethodTest methodTest)
    {
        return test.InstrumentDilutionFactor == 0
            ? methodTest.InstrumentDilutionFactor
            : test.InstrumentDilutionFactor;
    }

    private static void ValidateInstrumentDilutionFactor(MethodPanelTestsDto test, MethodTest methodTest)
    {
        var instrumentDilutionFactor = ResolveInstrumentDilutionFactor(test, methodTest);
        var validInstrumentDilutionFactors = GetValidDilutionFactors(methodTest.Method.Technology);

        if (!validInstrumentDilutionFactors.Contains(instrumentDilutionFactor))
        {
            throw new ValidationException(Translations.InvalidInstrumentDilutionFactor);
        }

        var defaultDilution = methodTest.DilutionMethod == DilutionMethod.PreDiluted ? 1 : methodTest.DilutionFactor;
        if (methodTest.Method.PreventDilutionFactorsBelowDefault && instrumentDilutionFactor < defaultDilution)
        {
            throw new ValidationException(Translations.CombinedDilutionFactorBelowMinimum);
        }
    }

    private static List<int> GetValidDilutionFactors(AssayTechnology technology)
    {
        return technology switch
        {
            AssayTechnology.ImmunoCap => InstrumentDilutionFactors.ImmunoCap,
            AssayTechnology.EliA => InstrumentDilutionFactors.Elia,
            AssayTechnology.ImmunoCapIgG => InstrumentDilutionFactors.ImmunoCapIgG,
            AssayTechnology.ViewAllergy => InstrumentDilutionFactors.ViewAllergy,
            _ => throw new ArgumentOutOfRangeException(nameof(technology), technology, null)
        };
    }
}
