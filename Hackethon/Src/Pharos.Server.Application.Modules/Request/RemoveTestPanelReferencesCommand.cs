using MediatR;

namespace Pharos.Server.Application.Request;

public record RemoveTestPanelReferencesCommand(int[] PanelIds) : IRequest;
