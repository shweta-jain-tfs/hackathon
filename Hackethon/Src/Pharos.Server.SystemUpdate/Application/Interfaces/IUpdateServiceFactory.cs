using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Application.Interfaces;

internal interface IUpdateServiceFactory
{
    IUpdateService Resolve(UpdateSource source);
}