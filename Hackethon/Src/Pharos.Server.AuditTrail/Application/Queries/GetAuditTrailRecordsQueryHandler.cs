using Pharos.Server.Application.Queries;
using Pharos.Server.AuditTrail.Application.Interfaces;
using Pharos.Server.AuditTrail.Application.Services;
using Pharos.Server.AuditTrail.Domain;

namespace Pharos.Server.AuditTrail.Application.Queries
{
    public class GetAuditTrailRecordsQuery : SortedPartialQuery<AuditTrailRecordDto>
    {
        public string? SearchText { get; set; }
        public IEnumerable<string>? UserNames { get; set; }
        public DateTimeOffset? FromDate { get; set; }
        public DateTimeOffset? ToDate { get; set; }
        public bool BypassRowLimit { get; set; }
    }
    internal sealed class GetAuditTrailRecordsQueryHandler(IAuditTrailDbContext dbContext, DescriptionTemplateService descriptionService) : IRequestHandler<GetAuditTrailRecordsQuery, PartialQueryResult<AuditTrailRecordDto>>
    {
        public async Task<PartialQueryResult<AuditTrailRecordDto>> Handle(GetAuditTrailRecordsQuery request, CancellationToken cancellationToken)
        {
            var baseQuery = dbContext.AuditTrailRecords.AsNoTracking().IgnoreAutoIncludes();

            // Apply Date/User filters only
            var (query, searchRequested) = ApplyFiltering(baseQuery, request);

            // Sort by Created (date)
            query = query.OrderByDescending(x => x.Created);

            // Search across Description (Activity), Comment and EventDataString in-memory
            if (searchRequested)
            {
                var projectedList = await ApplyProjection(query, cancellationToken);

                var finalFiltered = projectedList.Where(r =>
                        (!string.IsNullOrEmpty(r.Description) && r.Description.Contains(request.SearchText!, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(r.Comment) && r.Comment.Contains(request.SearchText!, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(r.EventData) && r.EventData.Contains(request.SearchText!, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var paginated = Paginate(finalFiltered, request.StartIndex, request.Count);

                return new PartialQueryResult<AuditTrailRecordDto>(
                    paginated,
                    request.StartIndex,
                    finalFiltered.Count);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var paginatedQuery = query.Skip(request.StartIndex).Take(request.Count);
            var records = await ApplyProjection(paginatedQuery, cancellationToken);

            return new PartialQueryResult<AuditTrailRecordDto>(
                records,
                request.StartIndex,
                totalCount);
        }

        private static (IQueryable<AuditTrailRecord>, bool) ApplyFiltering(IQueryable<AuditTrailRecord> query, GetAuditTrailRecordsQuery request)
        {
            // Apply FromDate filter (GreaterThanOrEqualTo)
            if (request.FromDate.HasValue)
            {
                query = query.Where(r => r.Created >= request.FromDate.Value);
            }

            // Apply ToDate filter (LessThanOrEqualTo)
            if (request.ToDate.HasValue)
            {
                query = query.Where(r => r.Created <= request.ToDate.Value.AddDays(1));
            }

            // Apply multi-operator filter for UserNames
            if (request.UserNames?.Any() == true)
            {
                query = query.Where(r => r.CreatedBy != null && request.UserNames.Contains(r.CreatedBy));
            }

            if (string.IsNullOrWhiteSpace(request.SearchText))
            {
                return (query, false);
            }

            return (query, true);
        }

        private async Task<List<AuditTrailRecordDto>> ApplyProjection(IQueryable<AuditTrailRecord> query, CancellationToken ct)
        {
            var raw = await query
                .Select(r => new
                {
                    r.Id,
                    r.Comment,
                    r.Created,
                    r.CreatedBy,
                    r.EventDataString,
                    r.EventId
                })
                .ToListAsync(ct);

            return raw.Select(r => new AuditTrailRecordDto
            {
                Id = r.Id,
                Comment = r.Comment,
                Created = r.Created,
                CreatedBy = r.CreatedBy ?? "",
                EventData = r.EventDataString,
                Description = descriptionService.GetLocalizedDescription(r.EventId, r.EventDataString)
            }).ToList();
        }

        private static List<AuditTrailRecordDto> Paginate(IEnumerable<AuditTrailRecordDto> items, int startIndex, int count)
        {
            return items.Skip(startIndex).Take(count).ToList();
        }
    }
}