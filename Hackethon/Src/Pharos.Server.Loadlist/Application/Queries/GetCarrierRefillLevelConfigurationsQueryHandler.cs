using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.LoadList;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Queries;

internal class GetCarrierRefillLevelConfigurationsQueryHandler(ILoadListDbContext dbContext)
    : IRequestHandler<GetCarrierRefillLevelConfigurationsQuery, IReadOnlyList<CarrierRefillLevelConfigurationDto>>
{
    public async Task<IReadOnlyList<CarrierRefillLevelConfigurationDto>> Handle(
        GetCarrierRefillLevelConfigurationsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.InstrumentNumbers.Count == 0)
        {
            return [];
        }

        var instrumentNumbers = request.InstrumentNumbers.ToHashSet();

        var configurations = dbContext.LoadListConfigurations
            .AsNoTracking()
            .Where(configuration =>
                instrumentNumbers.Contains(configuration.InstrumentSerial) &&
                !string.IsNullOrEmpty(configuration.Identity));

        return await FilterCarrierConfigurations(configurations)
            .Select(configuration => new CarrierRefillLevelConfigurationDto(
                configuration.InstrumentSerial,
                configuration.ArticleType,
                configuration.Identity!,
                configuration.RefillLevel))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<LoadListConfiguration> FilterCarrierConfigurations(
        IQueryable<LoadListConfiguration> configurations)
    {
        return configurations.Where(configuration =>
            configuration.ArticleType == ArticleType.ImmunoCapCarrier ||
            configuration.ArticleType == ArticleType.EliaWellCarrier ||
            configuration.ArticleType == ArticleType.EliaCalibratorWellCarrier ||
            configuration.ArticleType == ArticleType.DilutionWellCarrier);
    }
}
