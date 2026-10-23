using Pharos.Server.Application.Queries;

namespace Pharos.Server.Reagents.Application.Queries.Inventory;

internal class GetCartridgesQueryValidator : SortedPartialQueryValidator<GetCartridgesQuery, CartridgeListItemDto>
{
    protected override bool UseCount(GetCartridgesQuery query)
    {
        return !query.BypassRowLimit;
    }

}
