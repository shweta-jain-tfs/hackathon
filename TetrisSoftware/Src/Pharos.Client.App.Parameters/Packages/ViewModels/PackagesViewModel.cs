using Pharos.Client.App.Parameters.Packages.Models;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Parameters.Packages.ViewModels;

internal class PackagesViewModel(IPackagesDataSource dataSource, IExceptionHandlerService exceptionHandler) : VirtualGridViewModel<Package>(dataSource, exceptionHandler)
{
    public Task<Dictionary<string, string>> GetMarketNamesAsync()
    {
        return dataSource.GetIntendedMarketFilterOptionsAsync();
    }
}
