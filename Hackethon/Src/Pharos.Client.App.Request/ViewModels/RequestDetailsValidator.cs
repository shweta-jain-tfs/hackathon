using FluentValidation;
using Pharos.Client.App.Request.Views;
using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.App.Request.ViewModels;

/// <summary>
///     Validator for <see cref="RequestDetailPageViewModel" />.
///     This is picked up automatically by the fluent validation validator
///     inside <see cref="Common.App.Validation.PhValidationSection" />
///     and <see cref="Common.App.Validation.PhSimpleValidationContext" />
/// </summary>
public class RequestDetailsValidator : AbstractValidator<RequestDetailsViewModel>
{
    public RequestDetailsValidator(IStringLocalizer<Translations> loc, IDateTime dateTime)
    {
        When(x => !string.IsNullOrWhiteSpace(x.SampleId), () =>
        {
            RuleSet(RequestDetailPage.HeaderViewName, () =>
            {
                RuleFor(x => x.SampleId)
                    .Must(SampleRequestIdValidator.IsValidSampleId)
                    .WithMessage(loc["RequestDetailPage_SampleId_Invalid"]);
            });

            RuleSet(RequestDetailPage.DetailsTabName, () =>
            {
                // Sample collection date validation
                RuleFor(x => x.SampleCollectionDate)
                    .Must(date => !date.HasValue || date.Value.Date <= dateTime.Now.Date)
                    .WithMessage(loc["RequestDetailPage_SampleDate_Future"]);

                RuleFor(x => x.RequestId)
                    .Must(SampleRequestIdValidator.IsValidRequestId)
                    .WithMessage(loc["RequestDetailPage_RequestId_Invalid"]);

                // Request date validation
                RuleFor(x => x.RequestDate)
                    .Must(date => !date.HasValue || date.Value.Date <= dateTime.Now.Date)
                    .WithMessage(loc["RequestDetailPage_RequestDate_Future"]);

                When(x => x.UiSettings.RequestSettings?.AllowEditingOfPredilutionFactor == true, () =>
                {
                    RuleFor(x => x.PreDilutionFactor)
                        .InclusiveBetween(1, 1000)
                        .WithMessage(loc["RequestDetailPage_PreDilution_ValidationRange"].Value.Replace("{0}", "1-1000"));
                });
            });

            RuleSet(RequestDetailPage.TubesTabName, () =>
            {
                RuleFor(x => x.SampleTubeType)
                    .NotNull()
                    .WithMessage(string.Format(loc["RequestDetailPage_ValidationError_ValueRequired"], loc["RequestDetailPage_SampleTube"]));
            });
        });
    }
}

public static class SampleRequestIdValidator
{
    public static bool IsValidSampleId(string? sampleId)
    {
        if (string.IsNullOrEmpty(sampleId))
        {
            return false;
        }

        if (!AllowedCharacters(sampleId))
        {
            return false;
        }

        return true;
    }

    public static bool IsValidRequestId(string? requestId)
    {
        if (string.IsNullOrEmpty(requestId))
        {
            return true;
        }

        if (!AllowedCharacters(requestId))
        {
            return false;
        }

        return true;
    }

    public static bool AllowedCharacters(string sampleId)
    {
        foreach (var character in sampleId)
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