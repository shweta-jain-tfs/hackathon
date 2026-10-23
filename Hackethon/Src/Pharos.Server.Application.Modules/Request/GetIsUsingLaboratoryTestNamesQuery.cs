using MediatR;

namespace Pharos.Server.Application.Request;

public record GetIsUsingLaboratoryTestNamesQuery : IRequest<bool>;