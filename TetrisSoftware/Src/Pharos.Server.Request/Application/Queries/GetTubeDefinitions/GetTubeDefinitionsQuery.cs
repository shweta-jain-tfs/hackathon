using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Sample;

namespace Pharos.Server.Request.Application.Queries.GetTubeDefinitions;

public record GetTubeDefinitionsQuery : IRequest<List<TubeDefinitionDto>>;

internal class GetTubeDefinitionsQueryHandler(IOptionsSnapshot<TubeTypesSettings> tubeSettings) : IRequestHandler<GetTubeDefinitionsQuery, List<TubeDefinitionDto>>
{
    public Task<List<TubeDefinitionDto>> Handle(GetTubeDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var result = tubeSettings.Value.TubeTypeList.Select(x => new TubeDefinitionDto
        {
            Id = x.TubeDefinitionId,
            Name = x.Name,
            SupportsVolumeCalculation = x.VolumeCalculation
        }).ToList();

        return Task.FromResult(result);
    }
}
