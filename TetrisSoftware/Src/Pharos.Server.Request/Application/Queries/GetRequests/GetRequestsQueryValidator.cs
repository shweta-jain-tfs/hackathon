using FluentValidation;
using Pharos.Server.Application.Queries;

namespace Pharos.Server.Request.Application.Queries.GetRequests;

internal class GetRequestsQueryValidator : SortedPartialQueryValidator<GetRequestsQuery, RequestSummaryDto>
{
    public GetRequestsQueryValidator()
    {
        When(x => x.FromDate != null && x.ToDate != null, () =>
            RuleFor(x => x.FromDate)
                .Must((q, p) => p!.Value <= q.ToDate!.Value)
                .WithMessage("Invalid sample date range."));
    }

    protected override bool UseCount(GetRequestsQuery query)
    {
        return !query.BypassRowLimit;
    }
}