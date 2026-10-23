using Pharos.Server.Application.Queries;

namespace Pharos.Server.LoadList.Application.Queries.GetConfigurations;

internal class GetLoadListConfigurationQueryValidator : SortedPartialQueryValidator<GetLoadListConfigurationQuery, LoadListConfigurationDto>
{
    protected override bool UseCount(GetLoadListConfigurationQuery query)
    {
        return !query.BypassRowLimit;
    }
}
