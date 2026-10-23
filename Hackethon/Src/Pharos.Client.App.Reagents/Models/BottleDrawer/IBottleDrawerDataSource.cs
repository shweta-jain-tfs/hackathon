using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Reagents.Models.BottleDrawer;

public interface IBottleDrawerDataSource
{
    Task<BottleDrawerDetails> GetBottleDrawerDetailsAsync();
    Task<LoadingPosition> GetBottleLoadingPosition(string barcode);
}
