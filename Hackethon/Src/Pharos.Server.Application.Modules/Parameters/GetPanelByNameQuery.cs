using MediatR;
using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Application.Parameters;

public record GetPanelByNameQuery(string PanelName) : IRequest<Panel?>;