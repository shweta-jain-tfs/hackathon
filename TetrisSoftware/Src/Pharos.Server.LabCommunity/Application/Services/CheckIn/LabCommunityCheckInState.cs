using Pharos.Server.LabCommunity.Application.Interfaces.CheckIn;

namespace Pharos.Server.LabCommunity.Application.Services.CheckIn;

internal sealed class LabCommunityCheckInState : ILabCommunityCheckInState
{
    private DateTimeOffset? _lastSuccessfulCheckInUtc;
    private bool _lastAttemptSucceeded;

    public void MarkSucceeded(DateTimeOffset utcNow)
    {
        _lastSuccessfulCheckInUtc = utcNow;
        _lastAttemptSucceeded = true;
    }

    public void MarkFailed(DateTimeOffset utcNow)
    {
        _lastAttemptSucceeded = false;
    }

    public bool HasRecentSuccessfulCheckIn(DateTimeOffset utcNow, TimeSpan checkInInterval)
    {
        return _lastAttemptSucceeded
            && _lastSuccessfulCheckInUtc.HasValue
            && _lastSuccessfulCheckInUtc.Value >= utcNow.Subtract(checkInInterval);
    }
}
