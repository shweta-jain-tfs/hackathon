using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Application.Services;

internal sealed class UpdateServiceFactory(IServiceProvider serviceProvider) : IUpdateServiceFactory
{
    public IUpdateService Resolve(UpdateSource source)
    {
        return source switch
        {
            UpdateSource.Usb => serviceProvider.GetRequiredKeyedService<IUpdateService>(nameof(UpdateSource.Usb)),
            UpdateSource.Remote => serviceProvider.GetRequiredKeyedService<IUpdateService>(nameof(UpdateSource.Remote)),
            _ => throw new NotSupportedException($"Unsupported update source: {source}")
        };
    }
}