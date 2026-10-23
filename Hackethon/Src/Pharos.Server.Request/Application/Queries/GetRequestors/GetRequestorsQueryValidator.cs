using Pharos.Server.Application.Queries;

namespace Pharos.Server.Request.Application.Queries.GetRequestors;

internal class GetRequestorsQueryValidator : SortedPartialQueryValidator<GetRequestorsQuery, RequestorSummaryDto>
{
    protected override bool UseCount(GetRequestorsQuery query)
    {
        return !query.BypassRowLimit;
    }
}
