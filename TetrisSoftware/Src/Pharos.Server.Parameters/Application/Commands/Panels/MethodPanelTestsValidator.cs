using FluentValidation;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Parameters.Application.Commands.Panels;

internal sealed class CreateMethodPanelCommandValidator : AbstractValidator<CreatePanelCommand>
{
    public CreateMethodPanelCommandValidator()
    {
        RuleFor(x => x.PanelName).NotEmpty().MaximumLength(Panel.PanelNameLength);
        RuleFor(x => x.PanelLongName).MaximumLength(Panel.PanelLongNameLength);
        RuleForEach(x => x.Tests).SetValidator(new MethodPanelTestsValidator());
    }
}

internal sealed class UpdateMethodPanelCommandValidator : AbstractValidator<UpdatePanelCommand>
{
    public UpdateMethodPanelCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.PanelName).NotEmpty().MaximumLength(Panel.PanelNameLength);
        RuleFor(x => x.PanelLongName).MaximumLength(Panel.PanelLongNameLength);
        RuleForEach(x => x.Tests).SetValidator(new MethodPanelTestsValidator());
    }
}

internal sealed class MethodPanelTestsValidator : AbstractValidator<MethodPanelTestsDto>
{
    public MethodPanelTestsValidator()
    {
        RuleFor(x => x.TestName).NotEmpty().MaximumLength(Constants.TestNameLength);
        RuleFor(x => x.MethodName).NotEmpty().MaximumLength(Constants.MethodNameLength);
        RuleFor(x => x.Replicates).InclusiveBetween(1, 5);
        RuleFor(x => x.InstrumentDilutionFactor).GreaterThanOrEqualTo(0);
    }
}
