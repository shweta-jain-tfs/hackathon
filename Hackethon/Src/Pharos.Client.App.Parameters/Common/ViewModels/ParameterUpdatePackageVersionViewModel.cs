using Pharos.Client.App.Parameters.Common.Models;

namespace Pharos.Client.App.Parameters.Common.ViewModels;

public class ParameterUpdatePackageVersionViewModel(ParameterUpdatePackageVersionDataSource dataSource) : ViewModelBase
{
    public async Task<string?> GetVersion()
    {
        return await dataSource.GetVersion();
    }
}