using MediatR;
using Pharos.Contracts.Parameters;
using Pharos.Server.Application.Parameters;

namespace Pharos.Server.Parameters.Application.Queries;

public record InstrumentDilutionDto(int DilutionFactor, bool IsValid);

public record GetInstrumentDilutionsQuery : IRequest<List<InstrumentDilutionDto>>
{
    public int PreDilutionFactor { get; set; }
    public required string MethodName { get; set; }
    public required string TestName { get; set; }
}

internal class GetInstrumentDilutionsQueryHandler(IMethodsService methodsService) : IRequestHandler<GetInstrumentDilutionsQuery, List<InstrumentDilutionDto>>
{
    public async Task<List<InstrumentDilutionDto>> Handle(GetInstrumentDilutionsQuery request, CancellationToken cancellationToken)
    {
        var (preventDilutionFactorsBelowDefault, defaultDilutionFactor, technology) = await methodsService.GetDefaultInstrumentDilutionFactor(request.TestName, request.MethodName);

        var preDefinedList = GetPredefinedList(technology);

        if (!preventDilutionFactorsBelowDefault)
        {
            return preDefinedList.Select(x => new InstrumentDilutionDto(x, true)).ToList();
        }

        var lowestInstrumentDilutionFactor = defaultDilutionFactor / request.PreDilutionFactor;

        return preDefinedList.Select(x => new InstrumentDilutionDto(x, x >= lowestInstrumentDilutionFactor)).ToList();
    }

    private static List<int> GetPredefinedList(AssayTechnology? technology)
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