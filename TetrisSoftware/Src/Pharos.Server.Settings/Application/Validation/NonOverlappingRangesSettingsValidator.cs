using System.Globalization;
using Pharos.Server.Settings.Application.Commands;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Application.Validation;

internal sealed class NonOverlappingRangesSettingsValidator : ISettingsCrossValidator
{
    public IEnumerable<FailedSettingImportError> Validate(IReadOnlyDictionary<string, Option> options)
    {
        var emittedRules = new HashSet<string>(StringComparer.Ordinal);

        // The same rule is attached to each affected range option, so validate it only once.
        foreach (var validation in options.Values
            .SelectMany(option => option.NonOverlappingRanges)
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

            foreach (var key in validation.RangeKeys)
            {
                if (options.TryGetValue(key, out var option))
                {
                    yield return new FailedSettingImportError(
                        option.Key,
                        option.Value ?? string.Empty,
                        ToImportError(validation.Error));
                }
            }
        }
    }

    private static bool IsEnabled(
        NonOverlappingRangesValidation validation,
        IReadOnlyDictionary<string, Option> options)
    {
        return validation.EnabledWhen
            .GroupBy(dependency => dependency.MainOptionKey)
            .All(group => group.Any(dependency => IsDependencyMatched(dependency, options)));
    }

    private static bool IsDependencyMatched(
        Dependency dependency,
        IReadOnlyDictionary<string, Option> options)
    {
        return options.TryGetValue(dependency.MainOptionKey, out var option) &&
               MatchesDependencyValue(option.Value, dependency.MainOptionValue);
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
        IReadOnlyDictionary<string, Option> options,
        string startKey,
        string lengthKey,
        out int start,
        out int end)
    {
        start = 0;
        end = 0;

        if (!options.TryGetValue(startKey, out var startOption) ||
            !options.TryGetValue(lengthKey, out var lengthOption) ||
            !int.TryParse(startOption.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var startPosition) ||
            !int.TryParse(lengthOption.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var length) ||
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

    private static SettingImportError ToImportError(string error)
    {
        return Enum.TryParse<SettingImportError>(error, out var parsed)
            ? parsed
            : SettingImportError.InvalidValue;
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
