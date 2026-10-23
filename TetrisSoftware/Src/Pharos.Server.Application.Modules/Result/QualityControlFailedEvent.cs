using MediatR;

namespace Pharos.Server.Application.Result;

public record QualityControlFailedEvent(string TubeId) : INotification;