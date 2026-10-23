namespace Pharos.Server.Result.Domain;

public enum QualityControlJudgement
{
    NotDefined = 0,
    Ok = 1,
    FlaggedHigh = 2,
    FlaggedLow = 3,
    High = 4,
    Low = 5,
    HighCv = 6,
}
