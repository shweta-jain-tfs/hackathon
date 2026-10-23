using Microsoft.EntityFrameworkCore;

namespace Pharos.Server.Application.Queries;

public abstract class PartialQueryHandlerBase<TQuery, TEntity, TDto> where TQuery : SortedPartialQuery<TDto>
{
    protected async Task<PartialQueryResult<TDto>> GetPartialQueryResultAsync(IQueryable<TEntity> query, TQuery request, CancellationToken cancellationToken)
    {
        // Add filtering
        query = AddFiltering(query, request);

        // Count records without fetching them
        var totalCount = await GetTotalCountAsync(query, cancellationToken);

        // Optional: count logical groups (different projection than items)
        var groupedCount = await GetGroupedCountAsync(query, request, cancellationToken);

        // Add sorting
        query = AddSorting(query, request.SortByProperty, request.SortByAscending);

        // Apply projection
        var projectedQuery = ApplyProjection(query);

        // Fetch items from database
        var items = await FetchItemsAsync(request.StartIndex, request.Count, projectedQuery, cancellationToken);

        //Attach extra information on items
        await AttachExtraInformation(items, cancellationToken);

        return new PartialQueryResult<TDto>(items, request.StartIndex, totalCount, groupedCount);
    }

    /// <summary>
    /// Optional logical count for grouped results (e.g. number of distinct group keys).
    /// Default is null (no grouped count).
    /// </summary>
    protected virtual Task<int?> GetGroupedCountAsync(IQueryable<TEntity> query, TQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult<int?>(null);
    }

    protected virtual Task AttachExtraInformation(List<TDto> items, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected abstract IQueryable<TEntity> AddFiltering(IQueryable<TEntity> query, TQuery request);

    protected abstract IQueryable<TEntity> AddSorting(IQueryable<TEntity> query, string? sortProperty, bool ascending);

    private static async Task<int> GetTotalCountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken)
    {
        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    protected abstract IQueryable<TDto> ApplyProjection(IQueryable<TEntity> query);

    private static async Task<List<TDto>> FetchItemsAsync(int startIndex, int count, IQueryable<TDto> projectedQuery, CancellationToken cancellationToken)
    {
        if (startIndex > 0)
        {
            projectedQuery = projectedQuery.Skip(startIndex);
        }

        projectedQuery = projectedQuery.Take(count);

        return await projectedQuery.ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}