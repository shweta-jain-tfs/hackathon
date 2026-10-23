using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.Search.Models;
using Pharos.Client.App.Search.Services;
using Pharos.Client.App.Search.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;

namespace Pharos.Client.App.Search;

public class SearchModule : IClientAppModule
{
    private SearchModule()
    {

    }

    public static IClientAppModule Instance { get; } = new SearchModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        //DataServices
        services.AddScoped<ISearchResultDataSource, SearchResultDataSource>();
        services.AddKeyedScoped<IScannedBarcodeService, SearchScannedBarcodeService>(ScannedBarcodeKeys.SearchScannedBarcodeService);

        //ViewModels
        services.AddTransient<SearchResultListPageViewModel>();
    }
}