using Pharos.Server.Application.Queries;

namespace Pharos.Server.AuditTrail.Application.Queries;

internal class GetAuditTrailRecordsQueryValidator : SortedPartialQueryValidator<GetAuditTrailRecordsQuery, AuditTrailRecordDto>
{
    protected override bool UseCount(GetAuditTrailRecordsQuery query)
    {
        return !query.BypassRowLimit;
    }
}
