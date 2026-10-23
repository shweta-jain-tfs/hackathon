using MediatR;
using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Application.Sample;

public record UnloadSampleTubesCommand(int RequestId, RequestUid RequestUid) : IRequest;