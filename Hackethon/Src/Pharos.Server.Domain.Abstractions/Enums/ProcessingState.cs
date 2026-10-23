namespace Pharos.Server.Domain.Enums;

/// <summary>
/// Specifies the processing type.
/// </summary>
public enum ProcessingState
{
    None,
    UnableToStart,
    Scheduled,
    Started,
    Pipetted,
    Aborted
}