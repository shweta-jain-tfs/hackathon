using System.Text;
using FluentValidation;
using Pharos.Server.Localization;
using Pharos.Server.Request.Application.Commands.CreateUpdateRequest;

namespace Pharos.Server.Request.Application.Commands.RequestGenerator;

internal class GenerateRequestsCommandValidator : AbstractValidator<GenerateRequestsCommand>
{
    private static readonly CompositeFormat _numberOfDigitsFormat = CompositeFormat.Parse(Translations.RequestGenerator_NumberOfDigits);
    private static readonly CompositeFormat _sampleIdStartOffsetFormat = CompositeFormat.Parse(Translations.RequestGenerator_SampleIdStartOffset);
    private static readonly CompositeFormat _amountToGenerateFormat = CompositeFormat.Parse(Translations.RequestGenerator_AmountToGenerate);
    private static readonly CompositeFormat _preDilutionFactorFormat = CompositeFormat.Parse(Translations.RequestGenerator_PreDilutionFactor);
    private static readonly CompositeFormat _trayStartOffsetFormat = CompositeFormat.Parse(Translations.RequestGenerator_TrayStartOffset);
    private static readonly CompositeFormat _samplesPerTrayFormat = CompositeFormat.Parse(Translations.RequestGenerator_SamplesPerTray);

    public GenerateRequestsCommandValidator()
    {
        RuleFor(c => c.GenerateRequestsDto).NotNull();

        When(c => c.GenerateRequestsDto != null, () =>
        {
            // Sample ID format
            RuleFor(c => c.GenerateRequestsDto.SampleIdHeadingCharacters)
                .Must(SampleRequestIdValidator.AllowedCharacters)
                .MaximumLength(7);

            RuleFor(c => c.GenerateRequestsDto.SampleIdNumberOfDigits)
                .InclusiveBetween(1, 16)
                .WithMessage(string.Format(null, _numberOfDigitsFormat, 1, 16));

            RuleFor(c => c.GenerateRequestsDto.SampleIdStartOffset)
                .InclusiveBetween(0, 9999999)
                .WithMessage(string.Format(null, _sampleIdStartOffsetFormat, 0, 9999999));

            RuleFor(c => c.GenerateRequestsDto.SampleIdTrailingCharacters)
                .Must(SampleRequestIdValidator.AllowedCharacters)
                .MaximumLength(7);

            // Generation options
            RuleFor(c => c.GenerateRequestsDto.AmountToGenerate)
                .InclusiveBetween(1, 9999)
                .WithMessage(string.Format(null, _amountToGenerateFormat, 1, 9999));

            RuleFor(c => c.GenerateRequestsDto.PreDilutionFactor)
                .InclusiveBetween(1, 1000)
                .WithMessage(string.Format(null, _preDilutionFactorFormat, 1, 1000));
            When(c => c.GenerateRequestsDto.PlaceSamplesInTrays, () =>
            {
                RuleFor(c => c.GenerateRequestsDto.TraysStartOffset)
                    .InclusiveBetween(0, 9999)
                    .WithMessage(string.Format(null, _trayStartOffsetFormat, 0, 9999));
                RuleFor(c => c.GenerateRequestsDto.SamplesPerTray)
                    .InclusiveBetween(1, 50)
                    .WithMessage(string.Format(null, _samplesPerTrayFormat, 1, 50));
            });
            // Tests
            RuleFor(c => c.GenerateRequestsDto.Tests)
                .NotEmpty()
                .WithMessage(Translations.RequestGenerator_AtLeastOneTest);

            RuleForEach(c => c.GenerateRequestsDto.Tests)
                .SetValidator(new TestDetailsValidator());
        });
    }
}

internal class TestDetailsValidator : AbstractValidator<TestDetails>
{
    private static readonly CompositeFormat _replicatesFormat = CompositeFormat.Parse(Translations.RequestGenerator_Replicates);

    public TestDetailsValidator()
    {
        RuleFor(t => t.MethodName)
            .NotEmpty()
            .WithMessage(Translations.RequestGenerator_MethodNameRequired);

        RuleFor(t => t.TestName)
            .NotEmpty()
            .WithMessage(Translations.RequestGenerator_TestNameRequired);

        RuleFor(t => t.Replicates)
            .InclusiveBetween(1, 20)
            .WithMessage(string.Format(null, _replicatesFormat, 1, 20));
    }
}

