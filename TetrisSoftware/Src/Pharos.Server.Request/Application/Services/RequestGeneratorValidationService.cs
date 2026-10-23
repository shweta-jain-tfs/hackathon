using FluentValidation;
using Pharos.Contracts.Parameters;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Localization;
using Pharos.Server.Request.Application.Commands.RequestGenerator;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Services;

internal sealed class RequestGeneratorValidationService(IMethodsService methods) : IRequestGeneratorValidationService
{
    private static readonly int[] _genericAllowedDilutionFactors = [1, 2, 3, 4, 5, 10, 20, 50, 100, 200, 1000, 10000];
    private const int _trayCapacity = 50;

    public async Task ValidateAsync(
        GenerateRequestsDto request,
        CancellationToken cancellationToken)
    {
        await ValidateTestsAsync(request.Tests, request.PreDilutionFactor);

        if (request.PlaceSamplesInTrays)
        {
            ValidateTrayPlacements(request);
        }
    }

    private async Task ValidateTestsAsync(
        IReadOnlyList<TestDetails> tests,
        int preDilutionFactor)
    {
        var methodNames = tests.Select(t => t.MethodName).ToHashSet();
        var testNames = tests.Select(t => t.TestName).ToHashSet();

        var fetchedMethods = await methods.GetMethods(
            x => methodNames.Contains(x.MethodName),
            x => new MethodValidationDto
            {
                MethodName = x.MethodName,
                Technology = x.Technology,
                PreventDilutionFactorsBelowDefault = x.PreventDilutionFactorsBelowDefault,
                Tests = x.Tests
                    .Where(t => t.Active && testNames.Contains(t.TestName))
                    .Select(t => new MethodTestValidationDto
                    {
                        Name = t.TestName,
                        DilutionMethod = t.DilutionMethod,
                        DilutionFactor = t.DilutionFactor
                    })
                    .ToList()
            });

        if (methodNames.Count > 0 && (fetchedMethods == null || fetchedMethods.Count == 0 || !methodNames.All(mn => fetchedMethods.Any(m => m.MethodName == mn))))
        {
            throw new ValidationException(Translations.RequestGenerator_InvalidMethodName);
        }

        foreach (var test in tests)
        {
            if (tests.Any(x => x != test && x.MethodName == test.MethodName && x.TestName == test.TestName))
            {
                throw new ValidationException(Translations.TestOrderDuplicate);
            }

            var method = fetchedMethods!.First(m => m.MethodName == test.MethodName);

            var methodTest = method.Tests.FirstOrDefault(t => t.Name == test.TestName);
            if (methodTest == null)
            {
                throw new ValidationException(Translations.RequestGenerator_InvalidTestName);
            }

            var dilutionFactorValid = method.Technology switch
            {
                AssayTechnology.ImmunoCap => InstrumentDilutionFactors.ImmunoCap.Contains(test.InstrumentDilutionFactor),
                AssayTechnology.EliA => InstrumentDilutionFactors.Elia.Contains(test.InstrumentDilutionFactor),
                AssayTechnology.ImmunoCapIgG => InstrumentDilutionFactors.ImmunoCapIgG.Contains(test.InstrumentDilutionFactor),
                AssayTechnology.ViewAllergy => InstrumentDilutionFactors.ViewAllergy.Contains(test.InstrumentDilutionFactor),
                _ => _genericAllowedDilutionFactors.Contains(test.InstrumentDilutionFactor)
            };

            if (!dilutionFactorValid)
            {
                throw new ValidationException(Translations.RequestGenerator_InvalidInstrumentDilutionFactor);
            }

            if (method.PreventDilutionFactorsBelowDefault)
            {
                var effectiveDefault = methodTest.DilutionMethod == DilutionMethod.PreDiluted
                    ? 1
                    : methodTest.DilutionFactor;

                if (preDilutionFactor * test.InstrumentDilutionFactor < effectiveDefault)
                {
                    throw new ValidationException(Translations.RequestGenerator_TotalDilutionFactorTooLow);
                }
            }
        }
    }

    private static void ValidateTrayPlacements(GenerateRequestsDto options)
    {
        var trayCount = (int)Math.Ceiling((double)options.AmountToGenerate / options.SamplesPerTray);
        var lastTrayId = options.TraysStartOffset + trayCount - 1;
        if (lastTrayId > 9999)
        {
            throw new ValidationException(Translations.RequestGenerator_TrayIdsExceedSupportedRange);
        }

        for (var sampleIndex = 0; sampleIndex < options.AmountToGenerate; sampleIndex++)
        {
            var tubePosition = (sampleIndex % options.SamplesPerTray) + 1;
            if (tubePosition > _trayCapacity)
            {
                throw new ValidationException(Translations.RequestGenerator_TrayPositionsExceedCapacity);
            }
        }
    }

    internal class MethodValidationDto
    {
        public string MethodName { get; init; } = string.Empty;
        public AssayTechnology Technology { get; init; }
        public bool PreventDilutionFactorsBelowDefault { get; init; }
        public List<MethodTestValidationDto> Tests { get; init; } = [];
    }

    internal class MethodTestValidationDto
    {
        public string Name { get; init; } = string.Empty;
        public DilutionMethod DilutionMethod { get; init; }
        public int DilutionFactor { get; init; }
    }
}