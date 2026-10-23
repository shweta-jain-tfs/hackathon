using MediatR;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetPackages;

public record GetPackageDetailsQuery(int Id) : IRequest<PackageDetailsDto>;


internal class GetPackageDetailsQueryHandler : IRequestHandler<GetPackageDetailsQuery, PackageDetailsDto>
{
    private readonly IParametersDbContext _context;

    public GetPackageDetailsQueryHandler(IParametersDbContext context)
    {
        _context = context;
    }

    public async Task<PackageDetailsDto> Handle(GetPackageDetailsQuery request, CancellationToken cancellationToken)
    {
        var package = await _context.Packages
            .Include(x => x.IntendedMarket)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (package == null)
        {
            throw new InvalidOperationException($"Package with ID {request.Id} not found.");
        }

        // Get articles associated with the package via PackageItem
        var packageItems = await _context.PackageItems
            .Where(pc => pc.PackageId == package.Id)
            .Include(pc => pc.Article)
            .Select(pc => new PackageItemDetailedDto
            {
                Id = pc.Article.Id,
                ArticleAmount = pc.ArticleAmount,
                Identity = pc.Article.Identity,
                FullName = pc.Article.FullName,
                ArticleNumber = pc.Article.ArticleNumber,
                ArticleType = pc.Article.ArticleType,
                Quantity = pc.Article.Quantity * (pc.Article.NumberOfCells ?? 1),
                MethodName = pc.Article.MethodName,
                UnitOfMeasure = pc.Article.UnitOfMeasure,
            })
            .ToListAsync(cancellationToken);

        return new PackageDetailsDto
        {
            Id = package.Id,
            PackageBarcode = package.PackageBarcode,
            OrderingNumber = package.OrderingNumber,
            PackageFullName = package.PackageFullName,
            IntendedMarket = package.IntendedMarket.Name,
            PackageItems = packageItems
        };
    }
}