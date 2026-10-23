namespace Pharos.Server.LabCommunity.Application;

public enum LabCommunityEnrollmentStatus
{
    Enrolled,
    AlreadyEnrolled,
    InvalidCredentials,
    Failed
}

internal sealed record LabCommunityEnrollmentResult(LabCommunityEnrollmentStatus Status, string? ApiKey = null)
{
    public static LabCommunityEnrollmentResult Enrolled(string apiKey)
    {
        return new LabCommunityEnrollmentResult(LabCommunityEnrollmentStatus.Enrolled, apiKey);
    }

    public static LabCommunityEnrollmentResult AlreadyEnrolled()
    {
        return new LabCommunityEnrollmentResult(LabCommunityEnrollmentStatus.AlreadyEnrolled);
    }

    public static LabCommunityEnrollmentResult InvalidCredentials()
    {
        return new LabCommunityEnrollmentResult(LabCommunityEnrollmentStatus.InvalidCredentials);
    }

    public static LabCommunityEnrollmentResult Failed()
    {
        return new LabCommunityEnrollmentResult(LabCommunityEnrollmentStatus.Failed);
    }
}
