using MediatR;

namespace Pharos.Server.Application.Request;

/// <summary>
/// This event is published when Pharos tries to send tests to an instrument and detects that
/// there is not enough sample volume in the tube to perform the tests.
/// This event can only be raised when the sample volume has been previously measured.
/// </summary>
public record NotEnoughSampleVolumeEvent(string InstrumentNumber, string TubeId) : INotification;
