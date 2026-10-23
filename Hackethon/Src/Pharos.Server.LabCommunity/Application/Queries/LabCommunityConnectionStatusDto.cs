namespace Pharos.Server.LabCommunity.Application.Queries;

/// <summary>
/// Represents the current LabCommunity connection status.
/// </summary>
public sealed record LabCommunityConnectionStatusDto(bool IsConnected);
