using MediatR;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetPackages;

public record GetIntendedMarketsQuery : IRequest<List<IntendedMarketOptionDto>>;

public class IntendedMarketOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal sealed class GetIntendedMarketsQueryHandler(IParametersDbContext context) : IRequestHandler<GetIntendedMarketsQuery, List<IntendedMarketOptionDto>>
{
    public async Task<List<IntendedMarketOptionDto>> Handle(GetIntendedMarketsQuery request, CancellationToken cancellationToken)
    {
        return await context.IntendedMarkets
            .Select(x => new IntendedMarketOptionDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);
    }
}
