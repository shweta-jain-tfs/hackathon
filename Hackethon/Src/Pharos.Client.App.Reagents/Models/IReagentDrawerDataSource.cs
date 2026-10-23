using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Reagents.Models;
public interface IReagentDrawerDataSource
{
    Task<ReagentDrawer> GetReagentDrawerDetailsAsync();
    Task UnlockReagentDrawerAsync();
    Task<ReagentContentDetail> GetReagentContentDetailAsync(string? barcode, string? reagentUdi);
}
