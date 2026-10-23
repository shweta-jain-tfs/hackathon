namespace Pharos.Server.Result.Domain;

public enum RunStatus
{
    Processing,
    Ready,
    Aborted,
    ReadyWithoutTests
}