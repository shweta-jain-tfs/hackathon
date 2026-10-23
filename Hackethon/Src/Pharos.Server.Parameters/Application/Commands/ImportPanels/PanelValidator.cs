using FluentValidation;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Parameters.Application.Commands.ImportPanels;

internal class PanelValidator : AbstractValidator<Panel>
{
    public PanelValidator()
    {
        RuleFor(x => x.PanelName).MaximumLength(Panel.PanelNameLength).NotEmpty();
        RuleFor(x => x.PanelLongName).MaximumLength(Panel.PanelLongNameLength);
        RuleForEach(x => x.Tests).SetValidator(new PanelTestValidator());
    }
}

internal class PanelTestValidator : AbstractValidator<PanelTest>
{
    public PanelTestValidator()
    {
        RuleFor(x => x.MethodTest).NotEmpty();
        RuleFor(x => x.MethodTest.Method).NotEmpty();
        RuleFor(x => x.MethodTest.TestName).MaximumLength(Constants.TestNameLength).NotEmpty();
        RuleFor(x => x.MethodTest.Method.MethodName).MaximumLength(Constants.MethodNameLength).NotEmpty();
        RuleFor(x => x.Replicates).InclusiveBetween(0, 20);
    }
}