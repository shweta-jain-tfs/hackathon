namespace Pharos.Server.LabCommunity.Application.Interfaces.CheckIn;

internal interface ILabCommunityCheckInState
{
    void MarkSucceeded(DateTimeOffset utcNow);

    void MarkFailed(DateTimeOffset utcNow);

    bool HasRecentSuccessfulCheckIn(DateTimeOffset utcNow, TimeSpan checkInInterval);
}
