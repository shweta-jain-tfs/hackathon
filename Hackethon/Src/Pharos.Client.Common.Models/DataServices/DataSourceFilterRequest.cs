namespace Pharos.Client.Common.Models.DataServices;

public record DataSourceFilterRequest
{
    /// <summary>
    /// The collection of filters for the request.
    /// </summary>
    public ICollection<DataSourceFilter> Filters { get; init; } = new List<DataSourceFilter>();



    /// <summary>
    /// The <see cref="CancellationToken"/> used to relay cancellation of the request.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}
