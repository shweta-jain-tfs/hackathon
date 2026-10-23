namespace Pharos.Server.Result.Domain;

public enum CurveControlJudgmentResult
{
    NotJudged = 0,
    OK = 1,
    Flagged = 2,
    NotOK = 3,
    ReRunNotOK = 4,
}
