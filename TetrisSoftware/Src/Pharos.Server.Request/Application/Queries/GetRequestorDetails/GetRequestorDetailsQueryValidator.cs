using FluentValidation;

namespace Pharos.Server.Request.Application.Queries.GetRequestorDetails;

internal class GetRequestorDetailsQueryValidator : AbstractValidator<GetRequestorDetailsQuery>
{
    public GetRequestorDetailsQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Requestor ID must be a positive number.");
    }
}