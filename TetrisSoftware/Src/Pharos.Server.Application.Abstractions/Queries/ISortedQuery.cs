using MediatR;

namespace Pharos.Server.Application.Queries;

public interface ISortedQuery<out TResponse> : IRequest<TResponse>
{
    /// <summary>
    /// Specifies which property represents the sort order.
    /// </summary>
    public string? SortByProperty { get; set; }

    /// <summary>
    /// Specifies the requested sort direction.
    /// </summary>
    public bool SortByAscending { get; set; }
}
