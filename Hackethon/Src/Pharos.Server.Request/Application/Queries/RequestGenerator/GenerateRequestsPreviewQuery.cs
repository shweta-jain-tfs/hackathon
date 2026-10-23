using MediatR;
using Pharos.Server.Request.Application.Commands.RequestGenerator;
using Pharos.Server.Request.Application.Helpers;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries.RequestGenerator;

internal sealed record GenerateRequestsPreviewQuery(GenerateRequestsDto GenerateRequestsDto) : IRequest<List<RequestPreviewDto>>;

internal sealed class GenerateRequestsPreviewQueryHandler(
    IRequestGeneratorValidationService validationService) : IRequestHandler<GenerateRequestsPreviewQuery, List<RequestPreviewDto>>
{
    public async Task<List<RequestPreviewDto>> Handle(GenerateRequestsPreviewQuery query, CancellationToken cancellationToken)
    {
        await validationService.ValidateAsync(query.GenerateRequestsDto, cancellationToken);

        var sampleIds = RequestGenerationHelper.GenerateSampleIds(query.GenerateRequestsDto);

        var trayPlacements = query.GenerateRequestsDto.PlaceSamplesInTrays
            ? RequestGenerationHelper.GenerateTrayPlacements(query.GenerateRequestsDto)
            : [];

        return RequestGenerationHelper.BuildPreview(sampleIds, query.GenerateRequestsDto, trayPlacements);
    }
}

internal sealed record RequestGenerationTrayPlacement(string TrayId, int TubePosition);
