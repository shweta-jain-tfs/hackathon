using MediatR;
namespace Pharos.Server.Application.Reagents;

public record GetCarrierContentQuery(string ReagentBarcode, string? CarrierUdi) : IRequest<ReagentContentDto?>;