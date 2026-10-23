using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Pharos.Server.Infrastructure.Database;
public interface IAuditableEntitySaveChangesInterceptor : ISaveChangesInterceptor
{
}
