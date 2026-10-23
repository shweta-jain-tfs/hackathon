using System.Globalization;
namespace Pharos.Client.App.Parameters.Packages.Models;

internal interface IPackagesDataSource : IPagedDataSource<Package>
{
    Task<Dictionary<string, string>> GetIntendedMarketFilterOptionsAsync();
}

internal class PackagesDataSource(HttpClient httpClient) : IPackagesDataSource
{
    public async Task<DataSourceResult<Package>> GetAsync(DataSourceRequest request)
    {
        var client = new PackagesClient(httpClient);
        string? searchText = null;
        List<int> selectedMarkets = [];
        SetFilter(request, ref searchText, selectedMarkets);

        var result = await client.GetPackagesAsync(searchText, selectedMarkets, request.StartIndex, request.Count, request.SortByProperty, request.SortByAscending);
        return new DataSourceResult<Package>(result.Items.ToList(), result.TotalCount);
    }

    public async Task<Dictionary<string, string>> GetIntendedMarketFilterOptionsAsync()
    {
        var client = new PackagesClient(httpClient);
        var intendedMarkets = await client.GetIntendedMarketsAsync();

        return intendedMarkets.ToDictionary(
            x => x.Id.ToString(CultureInfo.InvariantCulture),
            x => x.Name);
    }

    private static void SetFilter(DataSourceFilterRequest request, ref string? searchText, List<int> selectedMarkets)
    {
        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(Package.OrderingNumber))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                    case FilterOperator.EqualsTo:
                        searchText = f.FilterValue as string;
                        break;
                }

                continue;
            }

            if (f.PropertyName == nameof(Package.IntendedMarket))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        if (f.FilterValue is string marketId && int.TryParse(marketId, out var id))
                        {
                            selectedMarkets.Add(id);
                        }
                        break;
                }

                continue;
            }
        }
    }
}