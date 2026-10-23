using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Interfaces;
internal interface ISettingsPagesLoader
{
    Task<Dictionary<string, Page>> LoadAsync(string rootPageLabel, CancellationToken token);
    Task<Dictionary<string, Page>> LoadAllAsync(CancellationToken token);
    Task<Page?> TryGetAsync(string pageKey, string rootPageLabel, CancellationToken token);
}