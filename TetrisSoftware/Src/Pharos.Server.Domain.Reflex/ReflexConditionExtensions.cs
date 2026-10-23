using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Domain.Reflex;

public static class ReflexConditionExtensions
{
    /// <summary>
    /// Evaluates whether the reflex condition is true for the given test result.
    /// </summary>
    public static bool IsTrueFor(this ReflexCondition condition, ITestResult testResult)
    {
        switch (condition.ResultType)
        {
            case ResultType.Concentration:

                return condition.CompareType switch
                {
                    CompareType.Above => testResult.MeanConcentration > condition.ConcentrationCompareValue,
                    CompareType.Below => testResult.MeanConcentration < condition.ConcentrationCompareValue,
                    CompareType.Between => testResult.MeanConcentration > condition.ConcentrationCompareValue && testResult.MeanConcentration < condition.ConcentrationCompareValueUpperLimit,
                    _ => false
                };

            case ResultType.Class:
                return condition.CompareType switch
                {
                    CompareType.EqualTo => testResult.MeanClass == condition.ClassCompareValue,
                    _ => false
                };

            case ResultType.CutOff:
                return condition.CompareType switch
                {
                    CompareType.EqualTo => testResult.MeanCutOff == condition.CutOffCompareValue,
                    _ => false
                };

            case ResultType.Quotient:
                return condition.CompareType switch
                {
                    CompareType.Above => testResult.MeanQuotient > condition.QuotientCompareValue,
                    CompareType.Below => testResult.MeanQuotient < condition.QuotientCompareValue,
                    CompareType.Between => testResult.MeanQuotient > condition.QuotientCompareValue && testResult.MeanQuotient < condition.QuotientCompareValueUpperLimit,
                    _ => false
                };

            case ResultType.ConcentrationRaw:
                return condition.CompareType switch
                {
                    CompareType.Above => testResult.MeanConcentrationRaw > condition.ConcentrationRawCompareValue,
                    CompareType.Below => testResult.MeanConcentrationRaw < condition.ConcentrationRawCompareValue,
                    CompareType.Between => testResult.MeanConcentrationRaw > condition.ConcentrationRawCompareValue && testResult.MeanConcentration < condition.ConcentrationRawCompareValueUpperLimit,
                    _ => false
                };

            case ResultType.RU:
                return condition.CompareType switch
                {
                    CompareType.Below => testResult.MeanResponse < condition.RuCompareValue,
                    _ => false
                };

            case ResultType.Default:
            default:
                return false;
        }
    }
}
