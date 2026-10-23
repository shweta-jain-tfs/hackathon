using FluentValidation;
using Pharos.Server.Application.Queries;

namespace Pharos.Server.Result.Application.Queries.GetAnalyticalRuns;

internal class GetAnalyticalRunsQueryValidator : SortedPartialQueryValidator<GetAnalyticalRunsQuery, AnalyticalRunDto>
{
    public GetAnalyticalRunsQueryValidator()
    {
        When(x => x.FromStartDate != null && x.ToStartDate != null, () =>
            RuleFor(x => x.FromStartDate)
                .Must((q, p) => p!.Value <= q.ToStartDate!.Value)
                .WithMessage("Invalid date range."));
    }

    protected override bool UseCount(GetAnalyticalRunsQuery query)
    {
        return !query.BypassRowLimit;
    }

    protected override string[] GetAllowedSortProperties()
    {
        return
        [
            nameof(AnalyticalRunDto.MethodName),
            nameof(AnalyticalRunDto.StateStatus),
            nameof(AnalyticalRunDto.IssueStatus),
            nameof(AnalyticalRunDto.StartDateTime),
            nameof(AnalyticalRunDto.EndDateTime),
            nameof(AnalyticalRunDto.CustomRunId)
        ];
    }
}