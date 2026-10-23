using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Queries.GetConfigurations;

public class LoadListConfigurationDto
{
    public int Id { get; set; }
    public string InstrumentSerial { get; set; } = default!;
    public ArticleType? ArticleType { get; set; }
    public int? RefillLevel { get; set; }
    public int? TriggerLevel { get; set; }
    public ArticleQuantityUnit Unit { get; set; } 
    public List<string?>? Colors { get; set; }
    public string? FriendlyName { get; set; }
    public List<string?>? Texts { get; set; }
    public ArticleIndicatorShape Shape { get; set; }
    public LoadListItemType LoadListItemType { get; set; }
    public string? FullName { get; set; }
}
