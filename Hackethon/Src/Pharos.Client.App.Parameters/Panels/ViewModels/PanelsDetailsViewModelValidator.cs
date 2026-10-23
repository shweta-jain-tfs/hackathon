using FluentValidation;

namespace Pharos.Client.App.Parameters.Panels.ViewModels;

public sealed class PanelsDetailsViewModelValidator : AbstractValidator<PanelsDetailsViewModel>
{
    private const int _panelNameMaxLength = 16;
    private const int _panelLongNameMaxLength = 30;

    public PanelsDetailsViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.PanelName)
            .NotEmpty()
            .WithMessage(loc["PanelDetailsPage_PanelNameRequired"])
            .MaximumLength(_panelNameMaxLength);

        RuleFor(x => x.PanelLongName)
            .MaximumLength(_panelLongNameMaxLength);
    }
}
