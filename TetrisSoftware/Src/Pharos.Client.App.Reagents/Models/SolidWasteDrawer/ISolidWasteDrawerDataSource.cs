using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Reagents.Models.SolidWasteDrawer;

/// <summary>
/// Data source interface for solid waste drawer operations
/// </summary>
public interface ISolidWasteDrawerDataSource
{
    /// <summary>
    /// Gets the current state of the waste drawer
    /// </summary>
    Task<SolidWasteDrawerDetails> GetWasteDrawerDetailsAsync();

    /// <summary>
    /// Unlocks the waste drawer
    /// </summary>
    Task UnlockSolidWasteDrawerAsync();
}
