using Pharos.Server.Application.Queries;

namespace Pharos.Server.Reagents.Application.Queries.Inventory;

internal class GetInternalDilutionPlatesQueryValidator : SortedPartialQueryValidator<GetInternalDilutionPlatesQuery, InternalDilutionPlateDto>
{
    protected override bool UseCount(GetInternalDilutionPlatesQuery query)
    {
        return !query.BypassRowLimit;
    }

}
