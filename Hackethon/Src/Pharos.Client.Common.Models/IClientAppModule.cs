using Microsoft.Extensions.DependencyInjection;
using Pharos.Contracts;

namespace Pharos.Client.Common.Models;

/// <summary>
/// Represents an abstraction for the client-side application module.
/// </summary>
public interface IClientAppModule : IAppModule
{
    /// <summary>
    /// <see langword="true"/> if the module has pages that must be mapped as endpoints; otherwise, <see langword="false"/>.
    /// </summary>
    bool HasPages => true;

    /// <summary>
    /// The value indicating whether the module's pages must only be accessible on a specific client device type defined by <see cref="ClientDeviceType"/>.
    /// </summary>
    bool IsDeviceSpecific => false;

    /// <summary>
    /// If <see cref="IsDeviceSpecific"/> is <see langword="true"/>, this property returns the client device type for which the module's pages are accessible.
    /// </summary>
    ClientDeviceType? DeviceType => null;

    /// <summary>
    /// Registers module services.
    /// </summary>
    void AddServices(IServiceCollection services);
}