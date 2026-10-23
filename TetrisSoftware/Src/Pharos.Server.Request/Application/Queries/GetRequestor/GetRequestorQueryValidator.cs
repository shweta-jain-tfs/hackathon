using FluentValidation;
using Pharos.Server.Application.Queries;

namespace Pharos.Server.Request.Application.Queries.GetRequestor;

internal class GetRequestorQueryValidator : SortedPartialQueryValidator<GetRequestorQuery, RequestorDto>
{
    public GetRequestorQueryValidator()
    {
        RuleFor(x => x.RequestorId)
            .NotEmpty()
            .WithMessage("Requestor ID is required.");
    }

    protected override string[] GetAllowedSortProperties()
    {
        return
        [
            nameof(RequestorDto.RequestorId),
            nameof(RequestorDto.Name)
        ];
    }
}