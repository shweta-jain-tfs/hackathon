using FluentValidation;
using Pharos.Server.Domain;
using Pharos.Server.Localization;

namespace Pharos.Server.Sample.Application.Commands.CreateSampleTray;

internal class CreateSampleTrayCommandValidator : AbstractValidator<CreateSampleTrayCommand>
{
    public CreateSampleTrayCommandValidator()
    {
        RuleFor(x => x.TrayId)
            .NotEmpty().WithMessage(Translations.TrayIdRequired);
        RuleFor(x => x.TrayId)
            .Length(Constants.TrayIdLength).Must(BeANumber).WithMessage(Translations.TrayIdLengthValidation);
    }

    private bool BeANumber(string value)
    {
        return int.TryParse(value, out _);
    }
}
