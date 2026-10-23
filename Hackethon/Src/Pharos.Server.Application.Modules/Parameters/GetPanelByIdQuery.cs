using MediatR;
using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Application.Parameters;

public record GetPanelByIdQuery(int PanelId) : IRequest<Panel?>;