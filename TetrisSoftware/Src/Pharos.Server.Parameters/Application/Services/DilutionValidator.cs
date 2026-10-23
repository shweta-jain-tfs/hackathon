using FluentValidation;
using Pharos.Contracts.Parameters;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Localization;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Services;

internal class DilutionValidator : IDilutionValidator
{
    private readonly IParametersDbContext _dbContext;

    public DilutionValidator(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Validate(int testId, int preDilutionFactor, int instrumentDilutionFactor)
    {
        var (preventDilutionFactorsBelowDefault, defaultDilutionFactor, technology) = await GetDefaultInstrumentDilutionFactor(testId);

        var validInstrumentDilutionFactors = GetValidDilutionFactors(technology);

        if (!validInstrumentDilutionFactors.Contains(instrumentDilutionFactor))
        {
            throw new ValidationException(Translations.InvalidInstrumentDilutionFactor);
        }

        if (preventDilutionFactorsBelowDefault && instrumentDilutionFactor * preDilutionFactor < defaultDilutionFactor)
        {
            throw new ValidationException(Translations.CombinedDilutionFactorBelowMinimum);
        }
    }

    private async Task<(bool PreventDilutionFactorsBelowDefault, int DefaultDilutionFactor, AssayTechnology Technology)> GetDefaultInstrumentDilutionFactor(int testId)
    {
        var test = await _dbContext.MethodTests.Include(x => x.Method).Where(x => x.Id == testId).AsNoTracking().FirstOrDefaultAsync();
        if (test != null)
        {
            return test.DilutionMethod == DilutionMethod.PreDiluted
                ? (test.Method.PreventDilutionFactorsBelowDefault, 1, test.Method.Technology)
                : (test.Method.PreventDilutionFactorsBelowDefault, test.DilutionFactor, test.Method.Technology);
        }

        throw new NotFoundException("Test not found.");
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
}