using MediatR;

namespace Pharos.Server.Application.Request;

public record RequestIdResponseDto(int RequestId, DateTimeOffset RequestCreatedDate, bool RequestExists, string? SampleId = null);

public record GetRequestIdQuery(string SampleId, DateTimeOffset? RequestCreatedDate = null) : IRequest<RequestIdResponseDto>;
