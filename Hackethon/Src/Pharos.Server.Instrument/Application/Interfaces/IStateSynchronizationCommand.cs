using MediatR;
using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Instrument.Application.Interfaces;

/// <summary>
/// Represents a request sent to the instrument during the state synchronization (immediately after the new session start).
/// </summary>
internal interface IStateSynchronizationCommand : IInstrumentCommand, IRequest
{

    /// <summary>
    /// The ID of the instrument communication session to which this request belongs.
    /// If it is not specified, the current session must be used.
    /// </summary>
    Guid? SessionId { get; }
}