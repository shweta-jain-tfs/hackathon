using System.Globalization;

namespace Pharos.Client.App.Settings.ViewModels;

internal sealed class NonOverlappingRangesSettingsViewModelValidator : ISettingsViewModelCrossValidator
{
    public IReadOnlyCollection<SettingUpdateError> ManagedErrors { get; } = [SettingUpdateError.SectionsOverlap];

    public IEnumerable<SettingsViewModelCrossValidationError> Validate(IReadOnlyDictionary<string, OptionViewModelBase> options)
    {
        var emittedRules = new HashSet<string>(StringComparer.Ordinal);

        // The same rule is attached to each affected range option, so validate it only once.
        foreach (var validation in options.Values
            .SelectMany(GetNonOverlappingRanges)
            .Where(validation => emittedRules.Add(CreateRuleSignature(validation))))
        {
            if (!IsEnabled(validation, options))
            {
                continue;
            }

            if (!TryGetRange(options, validation.FirstStartKey, validation.FirstLengthKey, out var firstStart, out var firstEnd) ||
                !TryGetRange(options, validation.SecondStartKey, validation.SecondLengthKey, out var secondStart, out var secondEnd))
            {
                continue;
            }

            if (!HasOverlap(firstStart, firstEnd, secondStart, secondEnd))
            {
                continue;
            }

            foreach (var key in GetRangeKeys(validation))
            {
                if (options.ContainsKey(key))
                {
                    yield return new SettingsViewModelCrossValidationError(key, ToUpdateError(validation.Error));
                }
            }
        }
    }

    private static IEnumerable<NonOverlappingRangesValidation> GetNonOverlappingRanges(OptionViewModelBase option)
    {
        return option.Model is Option model
            ? model.NonOverlappingRanges ?? []
            : [];
    }

    private static string[] GetRangeKeys(NonOverlappingRangesValidation validation)
    {
        return
        [
            validation.FirstStartKey,
            validation.FirstLengthKey,
            validation.SecondStartKey,
            validation.SecondLengthKey
        ];
    }

    private static bool IsEnabled(
        NonOverlappingRangesValidation validation,
        IReadOnlyDictionary<string, OptionViewModelBase> options)
    {
        return (validation.EnabledWhen ?? [])
            .GroupBy(dependency => dependency.MainOptionKey)
            .All(group => group.Any(dependency => IsDependencyMatched(dependency, options)));
    }

    private static bool IsDependencyMatched(
        Dependency dependency,
        IReadOnlyDictionary<string, OptionViewModelBase> options)
    {
        return options.TryGetValue(dependency.MainOptionKey, out var option) &&
               MatchesDependencyValue(option.ValueAsString, dependency.MainOptionValue);
    }

    private static bool MatchesDependencyValue(string? actualValue, string expectedValue)
    {
        if (bool.TryParse(actualValue, out var actualBool) && bool.TryParse(expectedValue, out var expectedBool))
        {
            return actualBool == expectedBool;
        }

        return string.Equals(actualValue, expectedValue, StringComparison.Ordinal);
    }

    private static bool TryGetRange(
        IReadOnlyDictionary<string, OptionViewModelBase> options,
        string startKey,
        string lengthKey,
        out int start,
        out int end)
    {
        start = 0;
        end = 0;

        if (!options.TryGetValue(startKey, out var startOption) ||
            !options.TryGetValue(lengthKey, out var lengthOption) ||
            !int.TryParse(startOption.ValueAsString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var startPosition) ||
            !int.TryParse(lengthOption.ValueAsString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var length) ||
            startPosition < 1 ||
            length < 1)
        {
            return false;
        }

        start = startPosition - 1;
        end = start + length;
        return true;
    }

    private static bool HasOverlap(int firstStart, int firstEnd, int secondStart, int secondEnd)
    {
        return !(firstEnd <= secondStart || secondEnd <= firstStart);
    }

    private static SettingUpdateError ToUpdateError(string error)
    {
        return Enum.TryParse<SettingUpdateError>(error, out var parsed)
            ? parsed
            : SettingUpdateError.ValidationError;
    }

    private static string CreateRuleSignature(NonOverlappingRangesValidation validation)
    {
        return string.Join(
            '\u001F',
            validation.FirstStartKey,
            validation.FirstLengthKey,
            validation.SecondStartKey,
            validation.SecondLengthKey,
            validation.Error);
    }
}
