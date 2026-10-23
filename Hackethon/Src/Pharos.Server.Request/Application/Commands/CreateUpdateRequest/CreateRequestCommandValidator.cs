using FluentValidation;
using Pharos.Server.Domain;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Request.Application.Commands.CreateUpdateRequest;

internal class CreateRequestCommandValidator : BaseUpdateRequestCommandValidator<CreateRequestCommand>
{
    public CreateRequestCommandValidator(IDateTime dateTime) : base(dateTime)
    {
        RuleFor(c => c.SampleId)
            .NotEmpty().MaximumLength(Constants.SampleIdMaxLength);

        RuleFor(c => c.SampleId)
            .Must(SampleRequestIdValidator.IsValidSampleId).WithMessage("Sample id contains invalid characters");

        RuleFor(c => c.RequestId)
            .Must(SampleRequestIdValidator.IsValidRequestId).WithMessage("Request id contains invalid characters");

        RuleForEach(c => c.TestOrders)
            .Must(t => t.Id == 0)
            .WithMessage("Id must be 0 for every test order you are trying to create.");
    }
}