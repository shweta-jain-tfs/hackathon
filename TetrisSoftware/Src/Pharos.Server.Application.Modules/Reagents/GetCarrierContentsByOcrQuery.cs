using MediatR;

namespace Pharos.Server.Application.Reagents
{

    public record GetCarrierContentsByOcrQuery(IReadOnlyList<string> OcrList) : IRequest<IReadOnlyDictionary<string, ReagentContentDto>>;
}