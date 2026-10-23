using MediatR;

namespace Pharos.Server.Application.TaskCard;

public record DoesTaskCardExistQuery(TaskCardType CardType, string? InstrumentNumber = null) : IRequest<bool>;