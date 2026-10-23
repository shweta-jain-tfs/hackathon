namespace Pharos.Server.Parameters.Application.Queries.GetMethods;

public class MethodDetailsDto
{
    //Method General
    public int Id { get; set; }
    public string FullMethodName { get; set; } = string.Empty;
    public string MethodName { get; set; } = default!;
    public int MethodVersion { get; set; }
    public AssayTechnology Technology { get; set; }
    public bool Enabled { get; set; }
    public string? MethodNameLis { get; set; }
    public int? DefaultLisChannelId { get; set; }
    public bool IsRuo { get; set; }
    public bool AllowMonthlyCalibration { get; set; }
    public IList<MethodClassTableDto>? ClassTables { get; set; }
    public IList<MethodCurveControlDto>? CurveControls { get; set; }
    public MethodCalibratorCurveDto? CalibratorCurve { get; set; }
    public MethodReportingDto? Report { get; set; }
}

public class MethodReportingDto
{
    public string? TextAboveCutOff { get; set; }
    public string? DefaultTextAboveCutOff { get; set; }
    public string? TextBelowCutOff { get; set; }
    public string? DefaultTextBelowCutOff { get; set; }
    public string? TextQuantitativeAbove { get; set; }
    public string? DefaultTextQuantitativeAbove { get; set; }
    public string? TextQuantitativeBelow { get; set; }
    public string? DefaultTextQuantitativeBelow { get; set; }
    public string? TextQuantitativeDoubtful { get; set; }
    public string? DefaultTextQuantitativeDoubtful { get; set; }
    public bool UseCalculatedTextQuantitativeAbove { get; set; }
    public double MinConcentrationToReport { get; set; }
    public double DefaultMinConcentrationToReport { get; set; }
}

public class MethodCalibratorCurveDto
{
    public int QuotientIndex { get; set; }
    public int MaxNumberOfDays { get; set; }
    public int DefaultMaxNumberOfDays { get; set; }
    public string TestName { get; set; } = default!;
    public List<MethodCalibrationPointDto>? MethodCalibrationPoint { get; set; }
}

public class MethodCalibrationPointDto
{
    /// <summary>
    /// Name of the calibrator point, for example CAL-0.70
    /// </summary>
    public string CalibratorName { get; set; } = default!;

    /// <summary>
    /// The expected concentration for this calibrator point, for example 0,70
    /// </summary>
    public double ExpectedConcentration { get; set; }

    /// <summary>
    /// Number of replicates to use for this calibrator point, for example 2
    /// </summary>
    public int Replicates { get; set; }

    /// <summary>
    /// See "Phadia Laboratory Systems Data Management Specification (DMS), 303781" in Docan
    /// </summary>
    public double IdealResponse { get; set; }

    public double ExpectedStdDev { get; set; }
    public double UpperResponseLimit { get; set; }
    public double LowerResponseLimit { get; set; }
    public double StdDevLimit { get; set; }
    public double StdDevPrecisionLimit { get; set; }
    public double? UpperLogarithmicLimit { get; set; }
    public double? LowerLogarithmicLimit { get; set; }
}

public class MethodClassTableDto
{
    /// <summary>
    /// Class table id (1-20)
    /// </summary>
    public int ClassTableId { get; set; }

    public string TextBelow { get; set; } = string.Empty;
    public IList<ClassTableRowInfoDto>? ClassTableRow { get; set; }
}

public class ClassTableRowInfoDto
{
    /// <summary>
    /// Class table row id in this class table (1-10)
    /// </summary>
    public int ClassTableRowId { get; set; }

    /// <summary>
    /// Lowest concentration to be "inside" this concentration range
    /// </summary>
    public double Concentration { get; set; }

    public string? TextAbove { get; set; }
}

public class MethodCurveControlDto
{
    /// <summary>
    /// Name of the curve control, for example CC-1
    /// </summary>
    public string CurveControlName { get; set; } = default!;

    /// <summary>
    /// The ID of the method test to use for this method's curve control, for example a_IgE for method sIgE
    /// </summary>
    public string MethodTestName { get; set; } = string.Empty;

    /// <summary>
    /// Number of replicates to use for this curve control, for example 1
    /// </summary>
    public int Replicates { get; set; }

    /// <summary>
    /// The expected concentration for this curve control, for example 0,70
    /// </summary>
    public double ExpectedConcentration { get; set; }

    /// <summary>
    /// Expected standard deviation, for example 0,084
    /// </summary>
    public double ExpectedStdDev { get; set; }

    /// <summary>
    /// Inner standard deviation limit, for example 2,2
    /// </summary>
    /// <remarks>
    /// Example of usage: Inner minimum concentration = 0,7 – (0,084 * 2,2) = 0,51.
    /// </remarks>
    public double InnerStdDevLimit { get; set; }

    /// <summary>
    /// Outer standard deviation limit, for example 3,5
    /// </summary>
    public double OuterStdDevLimit { get; set; }
}