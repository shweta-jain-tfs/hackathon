using Pharos.Client.App.Search.Models;
using Riok.Mapperly.Abstractions;

namespace Pharos.Client.App.Search;

[Mapper(RequiredEnumMappingStrategy = RequiredMappingStrategy.Source)]
internal static partial class SearchResultMapper
{
    [MapProperty(nameof(SearchResult.Match), nameof(SearchResultItem.Match), Use = nameof(ToNonNullString))]
    public static partial SearchResultItem ToSearchResultItem(this SearchResult source);

    [MapEnum(EnumMappingStrategy.ByName, FallbackValue = ClientSearchResultModule.Unknown)]
    public static partial ClientSearchResultModule ToClientSearchResultModule(this SearchResultModules source);

    [MapEnum(EnumMappingStrategy.ByName, FallbackValue = ClientSearchResultType.Unknown)]
    public static partial ClientSearchResultType ToClientSearchResultType(this SearchResultType source);

    private static string ToNonNullString(string? value)
    {
        return value ?? string.Empty;
    }
}
