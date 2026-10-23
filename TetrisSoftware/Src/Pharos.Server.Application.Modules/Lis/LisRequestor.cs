using Pharos.Server.Domain.Common;

namespace Pharos.Server.Application.Lis;

/// <summary>
/// Information about the person who ordered the tests (sent by LIS with test orders).
/// </summary>
public record LisRequestor
{
    public string RequestorId { get; init; } = string.Empty;
    public string? ShortcutCode { get; init; }
    public string? PhoneNumber { get; init; }
    [ProtectedData] public string? Comment { get; init; }
    [ProtectedData] public string? Name { get; init; }
    [ProtectedData] public string? Address { get; init; }
    public bool ReflexEnabled { get; init; }
    public string ZipCode { get; set; } = string.Empty;
    public string OrderingPhysicianCode { get; set; } = string.Empty;
    public bool HasNonUniqueRequestorId { get; set; }
}