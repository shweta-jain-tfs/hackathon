namespace Pharos.Client.Common.App.Helpers;

public static class ReflexConditionFormatter
{
    public static string Format(
        string? testName,
        ResultType resultType,
        CompareType compareType,
        double? concentrationValue = null,
        double? concentrationUpperValue = null,
        double? concentrationRawValue = null,
        double? concentrationRawUpperValue = null,
        double? quotientValue = null,
        double? quotientUpperValue = null,
        string? classValue = null,
        string? cutOffValue = null,
        double? ruValue = null)
    {
        var testNameText = testName ?? string.Empty;
        var resultTypeText = resultType != ResultType.Default
            ? resultType.ToLocalizedString()
            : string.Empty;

        var conditionText = resultType switch
        {
            ResultType.Concentration => FormatNumericCondition(compareType, concentrationValue, concentrationUpperValue),
            ResultType.ConcentrationRaw => FormatNumericCondition(compareType, concentrationRawValue, concentrationRawUpperValue),
            ResultType.Quotient => FormatNumericCondition(compareType, quotientValue, quotientUpperValue),
            ResultType.Class => FormatEqualCondition(compareType, classValue),
            ResultType.CutOff => FormatEqualCondition(compareType, cutOffValue),
            ResultType.RU => FormatNumericCondition(compareType, ruValue, null),
            _ => string.Empty
        };

        return $"{testNameText} {resultTypeText} {conditionText}";
    }

    private static string FormatNumericCondition(CompareType compareType, double? value, double? upperValue)
    {
        return compareType switch
        {
            CompareType.Above => $"> {value}",
            CompareType.Below => $"< {value}",
            CompareType.Between => FormatBetweenCondition(value, upperValue),
            _ => string.Empty
        };
    }

    private static string FormatBetweenCondition(double? minValue, double? maxValue)
    {
        var minPlaceholder = GetLocalizedString("ReflexCondition_MinPlaceholder");
        var maxPlaceholder = GetLocalizedString("ReflexCondition_MaxPlaceholder");

        var minText = minValue?.ToString() ?? $"[{minPlaceholder}]";
        var maxText = maxValue?.ToString() ?? $"[{maxPlaceholder}]";

        return $"{minText}-{maxText}";
    }

    private static string FormatEqualCondition(CompareType compareType, string? value)
    {
        return compareType switch
        {
            CompareType.EqualTo => $"= {value}",
            _ => string.Empty
        };
    }

    private static string GetLocalizedString(string key)
    {
        return Translations.ResourceManager.GetString(key) ?? $"[{key}]";
    }
}
