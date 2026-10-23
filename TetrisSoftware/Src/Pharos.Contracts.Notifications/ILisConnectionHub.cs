namespace Pharos.Contracts.Notifications;

/// <summary>
/// The strongly typed server-to-client RPC interface for notifications bound to lis.
/// See remarks for more information.
/// </summary>
/// <remarks>
/// </remarks>
public interface ILisConnectionHub : IHub
{
    Task LisStatusChanged();
}