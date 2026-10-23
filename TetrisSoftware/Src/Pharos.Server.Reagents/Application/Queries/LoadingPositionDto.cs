using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Reagents.Application.Queries;

public class LoadingPositionDto
{
    public LoadingPositionStatus Status { get; set; }
    public ArticleType ArticleType { get; set; }
    public int Position { get; set; }
    public List<string?> Colors { get; set; } = new();
    public int Id { get; set; }
    public int Quantity { get; set; }
    public ArticleQuantityUnit UnitOfMeasure { get; set; }
    public string? Name { get; set; }
    public string? LotNumber { get; set; }
    public DateTimeOffset? ExpiryDate { get; set; }
    public BottlePositionSize Size => ArticleType == ArticleType.StopSolutionBottle ||
                                        (ArticleType == ArticleType.WashConcentrateBottle && Position < 3) ? BottlePositionSize.Large : BottlePositionSize.Small;
}

public enum LoadingPositionStatus
{
    Found,
    NotFound,
    NotValidPair
}