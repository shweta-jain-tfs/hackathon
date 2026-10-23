using MediatR;

namespace Pharos.Server.Application.Queries;

public interface IPartialQuery<TDto> : IRequest<PartialQueryResult<TDto>>
{
    /// <summary>
    /// The start index of the first item requested.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// If set, the maximum number of items to be supplied.
    /// </summary>
    public int Count { get; set; }
}
