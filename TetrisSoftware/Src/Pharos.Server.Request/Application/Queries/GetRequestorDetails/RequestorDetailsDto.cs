namespace Pharos.Server.Request.Application.Queries.GetRequestorDetails;

public class RequestorDetailsDto
{
    public int Id { get; set; }
    public string RequestorId { get; set; } = default!;
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Comment { get; set; }
    public string? ZipCode { get; set; }
    public string? OrderingPhysicianCode { get; set; }
    public string? PhoneNumber { get; set; }
    public bool ReflexEnabled { get; set; }
    public string? ShortcutCode { get; set; }
    public DateTimeOffset? LatestSampleDate { get; set; }
    public int NumberOfReportedSamples { get; set; }
    public int NumberOfReportedTests { get; set; }
    public DateTimeOffset? CounterResetDate { get; set; }
    public DateTimeOffset LastModified { get; set; }
}