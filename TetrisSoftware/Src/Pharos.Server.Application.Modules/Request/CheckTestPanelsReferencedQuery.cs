using MediatR;

namespace Pharos.Server.Application.Request;

public record CheckTestPanelsReferencedQuery(int[] PanelIds) : IRequest<bool>;
