using MediatR;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// This event is raised when the instrument sends a message indicating that the status of a test order has changed.
/// This event is a critical message that must be processed immediately, not in the background.
/// That is why it is defined as IRequest, not INotification.
/// </summary>
public sealed record TestOrderStatusChangedEvent(TestOrderStatusChange Message) : IRequest;