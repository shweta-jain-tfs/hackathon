using Pharos.Server.Application.Settings.Lis;

namespace Pharos.Server.Lis.Application.Interfaces;

/// <summary>
/// Defines a LIS connection factory.
/// </summary>
public interface ILisConnectionFactory
{
    ILisConnection CreateConnection(int channelId, LisChannel channelSettings, LisConnectionRole role);
}
