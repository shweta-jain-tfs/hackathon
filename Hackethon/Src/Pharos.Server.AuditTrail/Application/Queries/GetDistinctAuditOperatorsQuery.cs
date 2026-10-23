using Pharos.Server.AuditTrail.Application.Interfaces;

namespace Pharos.Server.AuditTrail.Application.Queries;

public sealed class GetDistinctAuditOperatorsQuery : IRequest<IReadOnlyList<string>> { }

internal sealed class GetDistinctAuditOperatorsQueryHandler
    : IRequestHandler<GetDistinctAuditOperatorsQuery, IReadOnlyList<string>>
{
    private readonly IAuditTrailDbContext _db;
    public GetDistinctAuditOperatorsQueryHandler(IAuditTrailDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> Handle(GetDistinctAuditOperatorsQuery request, CancellationToken ct)
    {
        return await _db.AuditTrailRecords.AsNoTracking().IgnoreAutoIncludes()
            .Where(r => r.CreatedBy != null && r.CreatedBy != "")
            .Select(r => r.CreatedBy!)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(ct);
    }
}