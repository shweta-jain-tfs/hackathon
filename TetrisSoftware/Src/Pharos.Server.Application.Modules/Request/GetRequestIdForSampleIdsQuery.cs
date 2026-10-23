using MediatR;
using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Application.Request;

// The dictionary structure is key:RequestUid, value:requestId
public record GetRequestIdForSampleIdsQuery(List<RequestUid> RequestUidItems) : IRequest<IReadOnlyDictionary<(string SampleId, DateTimeOffset RequestCreatedDate), string>>;