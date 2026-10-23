namespace Pharos.Server.Domain.Enums;

/// <summary>
/// Specifies the status of sample tube.
/// </summary>
/// <remarks>
/// Do not modify numbers for the existing enum entries (they are persisted).
/// </remarks>
public enum SampleTubeStatus
{
    None = 0,

    NotIdentified = 1,

    InProgress = 2,

    Completed = 3,

    FollowUpReflex = 4,

    PartlyCompleted = 5,

    PipettedQc = 6

    //ToDo: Add more statuses as needed
}