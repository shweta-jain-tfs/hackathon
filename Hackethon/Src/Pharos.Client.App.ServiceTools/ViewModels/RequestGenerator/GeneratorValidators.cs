using FluentValidation;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.Services;
using Pharos.Client.App.ServiceTools.Views.RequestGenerator;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.ServiceTools.ViewModels.RequestGenerator;

public class RequestGeneratorTestValidator : AbstractValidator<RequestGeneratorTest>
{
    public RequestGeneratorTestValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.Replicates)
            .InclusiveBetween(1, 20)
            .WithMessage(string.Format(
                loc["RequestGenerator_ValidationError_Range"], 1, 20));
    }
}

public class GeneratorViewModelValidator : AbstractValidator<GeneratorViewModel>
{
    public GeneratorViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleSet(nameof(ConfigurationView), () =>
        {
            RuleFor(x => x.HeadingCharacters)
                .MaximumLength(GeneratorViewModel.MaxHeadingTrailingLength)
                .WithMessage(string.Format(loc["RequestGenerator_ValidationError_MaxLength"],
                    loc["RequestGenerator_HeadingCharacters"], GeneratorViewModel.MaxHeadingTrailingLength));

            RuleFor(x => x.HeadingCharacters)
                .Must(AllowedCharacters)
                .WithMessage(loc["RequestGenerator_InvalidCharacters"]);

            RuleFor(x => x.NumberOfDigits)
                .InclusiveBetween(GeneratorViewModel.MinNumberOfDigits, GeneratorViewModel.MaxNumberOfDigits)
                .WithMessage(string.Format(loc["RequestGenerator_ValidationError_Range"],
                    GeneratorViewModel.MinNumberOfDigits, GeneratorViewModel.MaxNumberOfDigits));

            RuleFor(x => x.SampleIdStartOffset)
                .InclusiveBetween(GeneratorViewModel.MinSampleIdStartOffset, GeneratorViewModel.MaxSampleIdStartOffset)
                .WithMessage(string.Format(loc["RequestGenerator_ValidationError_Range"],
                    GeneratorViewModel.MinSampleIdStartOffset, GeneratorViewModel.MaxSampleIdStartOffset));

            RuleFor(x => x.TrailingCharacters)
                .MaximumLength(GeneratorViewModel.MaxHeadingTrailingLength)
                .WithMessage(string.Format(loc["RequestGenerator_ValidationError_MaxLength"],
                    loc["RequestGenerator_TrailingCharacters"], GeneratorViewModel.MaxHeadingTrailingLength));

            RuleFor(x => x.TrailingCharacters)
                .Must(AllowedCharacters)
                .WithMessage(loc["RequestGenerator_InvalidCharacters"]);
        });

        RuleSet(nameof(TraySettingsView), () =>
        {
            When(x => x.PlaceSamplesInTrays, () =>
            {
                RuleFor(x => x.TrayStartOffset)
                    .InclusiveBetween(GeneratorViewModel.MinTrayStartOffset, GeneratorViewModel.MaxTrayStartOffset)
                    .WithMessage(string.Format(loc["RequestGenerator_ValidationError_Range"],
                        GeneratorViewModel.MinTrayStartOffset, GeneratorViewModel.MaxTrayStartOffset));

                RuleFor(x => x.SamplesPerTray)
                    .InclusiveBetween(GeneratorViewModel.MinSamplesPerTray, GeneratorViewModel.MaxSamplesPerTray)
                    .WithMessage(string.Format(loc["RequestGenerator_ValidationError_Range"],
                        GeneratorViewModel.MinSamplesPerTray, GeneratorViewModel.MaxSamplesPerTray));
            });
        });

        RuleSet(nameof(GeneratorView), () =>
        {
            RuleFor(x => x.AmountToGenerate)
                .InclusiveBetween(GeneratorViewModel.MinAmountToGenerate, GeneratorViewModel.MaxAmountToGenerate)
                .WithMessage(string.Format(loc["RequestGenerator_ValidationError_Range"],
                    GeneratorViewModel.MinAmountToGenerate, GeneratorViewModel.MaxAmountToGenerate));

            RuleFor(x => x.PreDilutionFactor)
                .InclusiveBetween(GeneratorViewModel.MinPreDilutionFactor, GeneratorViewModel.MaxPreDilutionFactor)
                .WithMessage(string.Format(loc["RequestGenerator_ValidationError_Range"],
                    GeneratorViewModel.MinPreDilutionFactor, GeneratorViewModel.MaxPreDilutionFactor));
        });
    }
    
    private static bool AllowedCharacters(string headingCharacters)
    {
        foreach (var character in headingCharacters)
        {
            var isAllowed =
                (character >= 32 && character <= 125 &&
                 character != '&' // ASCII 38
                 && character != '\\' // ASCII 92
                 && character != '^' // ASCII 94
                 && character != '|') // ASCII 124
                || (character >= 128 && character <= 254);

            if (!isAllowed)
            {
                return false;
            }
        }

        return true;
    }
}