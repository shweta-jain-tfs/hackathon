using System.Text;
using FluentValidation;
using MediatR;
using Pharos.Contracts.Parameters;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Localization;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.Panels;

public class CreatePanelCommand : IRequest<int>
{
    public string PanelName { get; set; } = string.Empty;
    public string PanelLongName { get; set; } = string.Empty;
    public bool Grouped { get; set; }
    public bool Enabled { get; set; }
    public List<MethodPanelTestsDto> Tests { get; set; } = [];
}

internal sealed class CreatePanelCommandHandler(IParametersDbContext context) : IRequestHandler<CreatePanelCommand, int>
{
    private static readonly CompositeFormat _panelAlreadyExistsFormat = CompositeFormat.Parse(Translations.PanelAlreadyExists);

    public async Task<int> Handle(CreatePanelCommand request, CancellationToken cancellationToken)
    {
        if (await context.Panels.AnyAsync(x => x.PanelName == request.PanelName, cancellationToken))
        {
            throw new ValidationException(string.Format(null, _panelAlreadyExistsFormat, request.PanelName));
        }

        ValidateDuplicateTests(request.Tests);

        var panel = new Panel
        {
            PanelName = request.PanelName,
            PanelLongName = request.PanelLongName,
            GroupedPanel = request.Grouped,
            Enabled = request.Enabled,
            Tests = []
        };

        var sortOrder = 1;
        foreach (var test in request.Tests.OrderBy(x => x.SortOrder))
        {
            var methodTest = await GetMethodTest(test, cancellationToken);
            panel.Tests.Add(CreatePanelTest(test, methodTest, sortOrder++));
        }

        context.Panels.Add(panel);
        await context.SaveChangesAsync(cancellationToken);
        return panel.Id;
    }

    internal async Task<MethodTest> GetMethodTest(MethodPanelTestsDto test, CancellationToken cancellationToken)
    {
        var methodTest = await context.MethodTests
            .Include(x => x.Method)
            .FirstOrDefaultAsync(x => x.Method.MethodName == test.MethodName && x.TestName == test.TestName, cancellationToken);

        return methodTest ?? throw new ValidationException(Translations.InvalidTestName);
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

    private static PanelTest CreatePanelTest(MethodPanelTestsDto test, MethodTest methodTest, int sortOrder)
    {
        var panelTest = new PanelTest
        {
            MethodTest = methodTest
        };

        ApplyPanelTestValues(panelTest, test, methodTest, sortOrder);
        return panelTest;
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
