using FluentValidation;

namespace Pharos.Client.App.Parameters.Panels.ViewModels;

public sealed class PanelTestViewModelValidator : AbstractValidator<PanelTestViewModel>
{
    public PanelTestViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.Replicates)
            .InclusiveBetween(1, 5)
            .WithMessage(loc["ReflexDetailPage_NumberOfReplicates_ValidationRange"].Value.Replace("{0}", "1-5"));
    }
}
