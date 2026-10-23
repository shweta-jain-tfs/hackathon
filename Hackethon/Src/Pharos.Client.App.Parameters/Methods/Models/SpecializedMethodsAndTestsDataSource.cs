namespace Pharos.Client.App.Parameters.Methods.Models;

public interface ISpecializedMethodsAndTestsDataSource : IPagedDataSource<SpecializedMethodOrTest>
{
    Task SaveAsync(ICollection<SaveSpecializedRequest> items, CancellationToken cancellationToken);
}

internal sealed class SpecializedMethodsAndTestsDataSource(HttpClient httpClient) : ISpecializedMethodsAndTestsDataSource
{
    public async Task<DataSourceResult<SpecializedMethodOrTest>> GetAsync(DataSourceRequest request)
    {
        string? name = null;
        var types = new List<string>();

        foreach (var filter in request.Filters)
        {
            if (filter.PropertyName == nameof(SpecializedMethodOrTest.Name) && filter.FilterOperator == FilterOperator.Contains)
            {
                name = filter.FilterValue as string;
                continue;
            }

            if (filter.PropertyName == nameof(SpecializedMethodOrTest.Type) && filter.FilterOperator == FilterOperator.EqualsTo)
            {
                if (filter.FilterValue is string type && !string.IsNullOrWhiteSpace(type))
                {
                    types.Add(MapTypeFilter(type));
                }
                else if (filter.FilterValue is SpecializedMethodOrTestType enumType)
                {
                    types.Add(enumType.ToString());
                }

                continue;
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={filter.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {filter.FilterOperator}");
        }

        var client = new MethodsClient(httpClient);
        var result = await client.GetSpecializedMethodsAndTestsAsync(
            name,
            types,
            request.StartIndex,
            request.Count ?? 0,
            request.SortByProperty,
            request.SortByAscending);

        return new DataSourceResult<SpecializedMethodOrTest>(result.Items.ToList(), result.TotalCount, result.GroupedCount);
    }

    public async Task SaveAsync(ICollection<SaveSpecializedRequest> items, CancellationToken cancellationToken)
    {
        var client = new MethodsClient(httpClient);
        await client.SaveSpecializedMethodsAndTestsAsync(new SaveSpecializedMethodsAndTestsCommand
        {
            Items = items
        }, cancellationToken);
    }

    private static string MapTypeFilter(string type)
    {
        foreach (var enumValue in Enum.GetValues<SpecializedMethodOrTestType>())
        {
            if (string.Equals(enumValue.ToLocalizedString(), type, StringComparison.Ordinal))
            {
                return enumValue.ToString();
            }
        }

        return type;
    }
}
