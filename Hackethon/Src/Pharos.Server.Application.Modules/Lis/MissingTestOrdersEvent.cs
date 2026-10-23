using MediatR;

namespace Pharos.Server.Application.Lis;

public record MissingTestOrdersEvent(string SampleId) : INotification;