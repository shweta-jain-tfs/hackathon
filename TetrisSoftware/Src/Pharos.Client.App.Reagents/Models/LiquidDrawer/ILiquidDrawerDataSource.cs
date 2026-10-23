using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Reagents.Models.LiquidDrawer;

public interface ILiquidDrawerDataSource
{
    Task<LiquidDrawerDetails> GetLiquidDrawerDetailsAsync();
}