using Microsoft.EntityFrameworkCore;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Settings.Application.Interfaces;

internal interface ISettingsDbContext : IDbContext, IDisposable
{
    DbSet<Setting> Settings { get; }
}
