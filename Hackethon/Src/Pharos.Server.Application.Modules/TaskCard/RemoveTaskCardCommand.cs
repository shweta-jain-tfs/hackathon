using MediatR;

namespace Pharos.Server.Application.TaskCard;

public record RemoveTaskCardCommand(TaskCardType CardType, string? InstrumentNumber = null) : IRequest;