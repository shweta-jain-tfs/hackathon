namespace Pharos.Server.Application.Queries;

public abstract class SortedPartialQuery<TDto> : IPartialQuery<TDto>, ISortedQuery<TDto>
{
    public int StartIndex { get; set; }
    public int Count { get; set; } = 10;
    public string? SortByProperty { get; set; }
    public bool SortByAscending { get; set; }
}