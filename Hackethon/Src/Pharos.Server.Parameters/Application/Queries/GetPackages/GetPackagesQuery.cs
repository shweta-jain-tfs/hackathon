using MediatR;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetPackages;

public class GetPackagesQuery : SortedPartialQuery<PackageDto>
{
    public string? SearchText { get; set; }
    public List<int>? SelectedMarkets { get; set; }
}

internal class GetPackagesQueryHandler : PartialQueryHandlerBase<GetPackagesQuery, Package, PackageDto>, IRequestHandler<GetPackagesQuery, PartialQueryResult<PackageDto>>
{
    private readonly IParametersDbContext _context;

    public GetPackagesQueryHandler(IParametersDbContext context)
    {
        _context = context;
    }

    public async Task<PartialQueryResult<PackageDto>> Handle(GetPackagesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Packages.Select(x => x).AsQueryable(); // TODO: Add .Include(PackageContent)

        return await GetPartialQueryResultAsync(query, request, cancellationToken);
    }

    protected override IQueryable<Package> AddFiltering(IQueryable<Package> query, GetPackagesQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchText = request.SearchText.Trim();

            query = query.Where(x =>
                x.OrderingNumber.Contains(searchText) ||
                (x.PackageFullName != null && x.PackageFullName.Contains(searchText)) ||
                x.PackageBarcode == searchText);

        }
        if (request.SelectedMarkets != null && request.SelectedMarkets.Count > 0)
        {
            query = from package in query
                    join market in _context.IntendedMarkets on package.IntendedMarketId equals market.Id
                    where request.SelectedMarkets.Contains(market.Id)
                    select package;
        }
        return query;
    }

    protected override IQueryable<Package> AddSorting(IQueryable<Package> query, string? sortProperty, bool ascending)
    {
        return query;
    }

    protected override IQueryable<PackageDto> ApplyProjection(IQueryable<Package> query)
    {
        return from package in query
               join market in _context.IntendedMarkets
               on package.IntendedMarketId equals market.Id
               select new PackageDto
               {
                   Id = package.Id,
                   PackageBarcode = package.PackageBarcode,
                   OrderingNumber = package.OrderingNumber,
                   PackageFullName = package.PackageFullName,
                   IntendedMarket = market.Name
               };
    }
}