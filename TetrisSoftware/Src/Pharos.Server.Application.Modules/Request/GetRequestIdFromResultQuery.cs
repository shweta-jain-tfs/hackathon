using MediatR;
using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Application.Request;

public record GetRequestIdFromResultQuery(RequestUid RequestUid) : IRequest<RequestIdResponseDto>;
