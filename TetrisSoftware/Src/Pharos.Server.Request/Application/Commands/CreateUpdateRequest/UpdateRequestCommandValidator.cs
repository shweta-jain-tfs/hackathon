using FluentValidation;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Request.Application.Commands.CreateUpdateRequest;

internal class UpdateRequestCommandValidator : BaseUpdateRequestCommandValidator<UpdateRequestCommand>
{
    public UpdateRequestCommandValidator(IDateTime dateTime) : base(dateTime)
    {
        RuleFor(c => c.Id)
            .GreaterThan(0)
            .WithMessage("Id for the request must be properly set.");

        RuleFor(c => c.RequestId)
            .Must(SampleRequestIdValidator.IsValidRequestId).WithMessage("Request id contains invalid characters");

        RuleFor(c => c.RequestDetailsQueriedAt)
            .GreaterThan(DateTimeOffset.MinValue)
            .WithMessage($"{nameof(UpdateRequestCommand.RequestDetailsQueriedAt)} must be properly set.");
    }
}