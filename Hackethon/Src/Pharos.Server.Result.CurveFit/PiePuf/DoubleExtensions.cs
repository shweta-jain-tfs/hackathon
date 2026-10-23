namespace Pharos.Server.Result.CurveFit.PiePuf;

internal static class DoubleExtensions
{
    public static bool EqualsOrCloseTo(this double value1, double value2, double relativeTolerance = 1e-9, double absoluteFallback = 1e-12)
    {
        // 1. Direct equality handle (covers positive/negative infinity and exact matches)
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (value1 == value2)
        {
            return true;
        }

        // 2. Calculate the actual difference
        var difference = Math.Abs(value1 - value2);

        // 3. Use absolute tolerance if numbers are close to zero
        if (Math.Abs(value1) < absoluteFallback || Math.Abs(value2) < absoluteFallback)
        {
            return difference < absoluteFallback;
        }

        // 4. Use relative tolerance scaled to the larger number
        var largest = Math.Max(Math.Abs(value1), Math.Abs(value2));
        return difference <= (largest * relativeTolerance);
    }
}