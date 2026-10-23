using FluentValidation;
using Pharos.Server.Domain;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Request.Application.Commands.CreateUpdateRequest;

internal abstract class BaseUpdateRequestCommandValidator<TBaseUpdateRequestCommand> : AbstractValidator<TBaseUpdateRequestCommand>
    where TBaseUpdateRequestCommand : BaseUpdateRequestCommand
{
    protected BaseUpdateRequestCommandValidator(IDateTime dateTime)
    {
        RuleFor(c => c.RequestId).MaximumLength(Domain.Request.RequestIdLength);

        // Sample collection date must not be in the future
        RuleFor(c => c.SampleCollectionDate)
            .Must(date => !date.HasValue || date.Value.Date <= dateTime.Now.Date)
            .WithMessage("Sample collection date cannot be in the future.");

        // Request date must not be in the future
        RuleFor(c => c.RequestDate)
            .Must(date => !date.HasValue || date.Value.Date <= dateTime.Now.Date)
            .WithMessage("Request date cannot be in the future.");

        RuleForEach(c => c.TestOrders).SetValidator(new NewUpdatedTestValidator());

        RuleFor(x => x.PreDilutionFactor).InclusiveBetween(0, Constants.MaxPredilutionFactor);

        RuleFor(x => x.Patient).SetValidator(new PatientValidator(dateTime)).When(x => x.Patient != null);
    }
}

internal class NewUpdatedTestValidator : AbstractValidator<NewUpdatedTest>
{
    public NewUpdatedTestValidator()
    {
        RuleFor(c => c.Id).GreaterThanOrEqualTo(0);
        RuleFor(c => c.TestName).NotEmpty().MaximumLength(Constants.TestNameLength);
        RuleFor(c => c.MethodName).NotEmpty().MaximumLength(Constants.MethodNameLength);
        RuleFor(c => c.NumberOfReplicates).InclusiveBetween(1, Constants.MaxReplicatesPerTest);
    }
}