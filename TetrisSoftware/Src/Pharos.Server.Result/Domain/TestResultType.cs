namespace Pharos.Server.Result.Domain;

/// <summary>
/// Lists available types of the test results. 
/// </summary>
/// <remarks>
/// Do not modify numbers for the existing enum entries (they are persisted).
/// </remarks>
public enum TestResultType
{
    Sample = 1,
    Calibrator = 2,
    CurveControl = 3,
    QualityControl = 4
}