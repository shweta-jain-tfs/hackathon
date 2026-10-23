namespace Pharos.Server.Lis.Application.Interfaces;

/// <summary>
/// Provides data for the StateChanged event.
/// </summary>
public sealed class LisConnectionStateChangedEventArgs(LisConnectionState newState) : EventArgs
{
    /// <summary>
    /// Gets the new connection state.
    /// </summary>
    public LisConnectionState NewState { get; } = newState;
}
