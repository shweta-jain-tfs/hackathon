using Pharos.Server.Application.Queries;

namespace Pharos.Server.Reagents.Application.Queries.Inventory;

internal class GetCarriersQueryValidator : SortedPartialQueryValidator<GetCarriersQuery, CarrierListItemDto>
{
    protected override bool UseCount(GetCarriersQuery query)
    {
        return !query.BypassRowLimit;
    }

}
