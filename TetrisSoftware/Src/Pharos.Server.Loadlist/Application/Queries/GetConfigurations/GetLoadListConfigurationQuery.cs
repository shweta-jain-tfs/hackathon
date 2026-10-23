using Pharos.Server.Application.Queries;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Queries.GetConfigurations;

public class GetLoadListConfigurationQuery : SortedPartialQuery<LoadListConfigurationDto>
{
    public string? InstrumentNumber { get; set; }
    public LoadListItemType? LoadListItemType { get; set; }
    public string? FriendlyName { get; set; }
    public bool BypassRowLimit { get; set; }
}

internal class GetLoadListConfigurationQueryHandler(ILoadListDbContext dbContext, IAlphanumericStringComparerProvider alphanumericStringComparerProvider) : PartialQueryHandlerBase<GetLoadListConfigurationQuery, LoadListConfiguration, LoadListConfigurationDto>, IRequestHandler<GetLoadListConfigurationQuery, PartialQueryResult<LoadListConfigurationDto>>
{
    public async Task<PartialQueryResult<LoadListConfigurationDto>> Handle(GetLoadListConfigurationQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.LoadListConfigurations.AsQueryable();

        var items = await GetPartialQueryResultAsync(query, request, cancellationToken);

        if (request.SortByProperty == nameof(LoadListConfigurationDto.FriendlyName))
        {
            items = SortByAlphaNumerical(items, request.SortByAscending, x => x.FriendlyName ?? string.Empty);
        }

        if (request.SortByProperty == nameof(LoadListConfigurationDto.Texts))
        {
            items = SortByAlphaNumerical(items, request.SortByAscending, x => x.Texts?.FirstOrDefault() ?? string.Empty);
        }

        return items;
    }

    private PartialQueryResult<LoadListConfigurationDto> SortByAlphaNumerical(
        PartialQueryResult<LoadListConfigurationDto> items, bool sortByAscending,
        Func<LoadListConfigurationDto, string> selectorFunc)
    {
        var comparer = alphanumericStringComparerProvider.GetComparer(descending: !sortByAscending);
        var sortedItems = items.Items.OrderBy(selectorFunc, comparer).ThenBy(x => x.Id).ToList();

        return new PartialQueryResult<LoadListConfigurationDto>(sortedItems, items.StartIndex, items.TotalCount);
    }

    protected override IQueryable<LoadListConfiguration> AddFiltering(IQueryable<LoadListConfiguration> query, GetLoadListConfigurationQuery request)
    {
        if (!string.IsNullOrEmpty(request.InstrumentNumber))
        {
            query = query.Where(c => c.InstrumentSerial == request.InstrumentNumber);
        }

        if (request.LoadListItemType.HasValue)
        {
            query = request.LoadListItemType.Value switch
            {
                LoadListItemType.Carrier => query.Where(c => c.ArticleType == ArticleType.ImmunoCapCarrier || c.ArticleType == ArticleType.EliaWellCarrier || c.ArticleType == ArticleType.EliaCalibratorWellCarrier || c.ArticleType == ArticleType.DilutionWellCarrier),
                LoadListItemType.Cartridge => query.Where(c => c.ArticleType == ArticleType.ConjugateCartridge || c.ArticleType == ArticleType.QualityControlCartridge || c.ArticleType == ArticleType.DiluentCartridge || c.ArticleType == ArticleType.DevelopmentSolutionCartridge || c.ArticleType == ArticleType.CurveControlCartridge || c.ArticleType == ArticleType.CalibratorCartridge || c.ArticleType == ArticleType.FluoroCVial),
                LoadListItemType.Bottle => query.Where(c => c.ArticleType == ArticleType.StopSolutionBottle || c.ArticleType == ArticleType.WashAdditiveBottle || c.ArticleType == ArticleType.WashConcentrateBottle || c.ArticleType == ArticleType.MaintenanceConcentrateBottle),
                LoadListItemType.DilutionPlate => query.Where(c => c.ArticleType == ArticleType.DilutionPlate),
                _ => query
            };
        }

        if (!string.IsNullOrEmpty(request.FriendlyName))
        {
            query = query.Where(x => x.FriendlyName != null && x.FriendlyName.Contains(request.FriendlyName));
        }
        return query;
    }

    protected override IQueryable<LoadListConfiguration> AddSorting(IQueryable<LoadListConfiguration> query, string? sortProperty, bool ascending)
    {
        if (sortProperty == nameof(LoadListConfiguration.FriendlyName))
        {
            query = ascending ? query.OrderBy(x => x.FriendlyName) : query.OrderByDescending(x => x.FriendlyName);
        }
        if (sortProperty == nameof(LoadListConfiguration.Texts))
        {
            query = ascending ? query.OrderBy(x => x.Text1).ThenBy(x => x.Text2).
                    ThenBy(x => x.Text3).ThenBy(x => x.Text4)
                : query.OrderByDescending(x => x.Text1).ThenBy(x => x.Text2)
                    .ThenBy(x => x.Text3).ThenBy(x => x.Text4);
        }
        if (sortProperty == nameof(LoadListConfiguration.ArticleType))
        {
            query = ascending ? query.OrderBy(x => x.ArticleType) : query.OrderByDescending(x => x.ArticleType);
        }
        if (sortProperty == nameof(LoadListConfiguration.RefillLevel))
        {
            query = ascending ? query.OrderBy(x => x.RefillLevel) : query.OrderByDescending(x => x.RefillLevel);
        }
        if (sortProperty == nameof(LoadListConfiguration.TriggerLevel))
        {
            query = ascending ? query.OrderBy(x => x.TriggerLevel) : query.OrderByDescending(x => x.TriggerLevel);
        }


        return query;
    }

    protected override IQueryable<LoadListConfigurationDto> ApplyProjection(IQueryable<LoadListConfiguration> query)
    {
        var configList = query.Select(c => new LoadListConfigurationDto
        {
            InstrumentSerial = c.InstrumentSerial,
            RefillLevel = c.RefillLevel,
            TriggerLevel = c.TriggerLevel,
            Id = c.Id,
            ArticleType = c.ArticleType,
            FriendlyName = c.FriendlyName,
            FullName = c.FullName,
            Texts = c.Texts,
            Shape = c.Shape,
            Unit = c.Unit,
            Colors = c.Colors,
            LoadListItemType = GetLoadListItemType(c.ArticleType)
        });

        return configList;
    }

    private static LoadListItemType GetLoadListItemType(ArticleType articleType)
    {
        return articleType switch
        {
            ArticleType.ImmunoCapCarrier => LoadListItemType.Carrier,
            ArticleType.EliaWellCarrier => LoadListItemType.Carrier,
            ArticleType.EliaCalibratorWellCarrier => LoadListItemType.Carrier,
            ArticleType.DilutionWellCarrier => LoadListItemType.Carrier,

            ArticleType.StopSolutionBottle => LoadListItemType.Bottle,
            ArticleType.WashAdditiveBottle => LoadListItemType.Bottle,
            ArticleType.WashConcentrateBottle => LoadListItemType.Bottle,

            ArticleType.DilutionPlate => LoadListItemType.DilutionPlate,
            _ => LoadListItemType.Cartridge
        };
    }
}