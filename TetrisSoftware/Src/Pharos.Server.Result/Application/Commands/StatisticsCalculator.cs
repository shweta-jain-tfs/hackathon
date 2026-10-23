namespace Pharos.Server.Result.Application.Commands;

internal class StatisticsCalculator
{
    private double _sumOfDataEx2;

    public int NumberOfData { get; private set; }

    public double Sum { get; private set; }

    public double Mean
    {
        get
        {
            if (NumberOfData > 0)
            {
                return Sum / NumberOfData;
            }
            return 0;
        }
    }

    public double SD
    {
        get
        {
            if (NumberOfData > 1)
            {
                return CalculateSD();
            }
            return 0;
        }
    }

    public double CV
    {
        get
        {
            if (NumberOfData > 1)
            {
                if (Mean > 0)
                {
                    return (CalculateSD() / Mean) * 100;
                }
            }
            return 0;
        }
    }



    /// <summary>
    /// Add one value to the calculation.
    /// </summary>                
    /// <param name="value">Value to add</param>
    public void Add(double? value)
    {
        if (value == null)
        {
            return;
        }
        var value2 = value ?? 0;

        Sum += value2;
        _sumOfDataEx2 += (value2 * value2);
        NumberOfData++;
    }



    /// <summary>
    /// Add a list of values to the calculation.
    /// </summary>                
    /// <param name="values">Values to add</param>
    public void Add(IEnumerable<double> values)
    {
        if (values == null)
        {
            return;
        }
        foreach (var value in values)
        {
            Sum += value;
            _sumOfDataEx2 += (value * value);
            NumberOfData++;
        }
    }



    /// <summary>
    /// Calculate Standard Deviation.
    /// </summary>        
    private double CalculateSD()
    {
        var result = ((NumberOfData * _sumOfDataEx2) - (Sum * Sum)) /
                        (NumberOfData * (NumberOfData - 1));

        if (result > 0)
        {
            result = Math.Sqrt(result);
        }
        else
        {
            result = 0;
        }

        return result;
    }
}
