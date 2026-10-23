using Pharos.Server.Application.Queries;

namespace Pharos.Server.Sample.Application.Queries.GetSampleTrayList;

internal class GetSampleTrayListQueryValidator : SortedPartialQueryValidator<GetSampleTrayListQuery, SampleTrayDto>
{
    protected override bool UseCount(GetSampleTrayListQuery query)
    {
        return !query.BypassRowLimit;
    }
}
