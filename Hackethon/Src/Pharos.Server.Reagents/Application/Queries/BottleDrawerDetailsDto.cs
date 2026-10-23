using System.Drawing;
using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Reagents.Application.Queries;

public class BottleDrawerDetailsDto
{
    public LockState LockState { get; set; }
    public List<BottlePositionDto> Positions { get; set; } = new();
}

public class BottlePositionDto
{
    public int Position { get; set; }
    public ArticleType ArticleType { get; set; }
    public BottlePositionSize Size => Position == 1 || Position == 3 ? BottlePositionSize.Large : BottlePositionSize.Small;
    public List<string> Colors => ArticleType switch
        {
        ArticleType.StopSolutionBottle => new() { ColorTranslator.ToHtml(Color.FromArgb(230, 134, 153)) },
        ArticleType.WashAdditiveBottle => new() { ColorTranslator.ToHtml(Color.FromArgb(141, 200, 232)) },
        ArticleType.WashConcentrateBottle => new() { ColorTranslator.ToHtml(Color.FromArgb(0, 91, 139)) },
        _ => new()
    };

    public List<BottleDetailDto> Bottles { get; set; } = new();
}

public class BottleDetailDto
{
    public int Id { get; set; }
    public int Position { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ReagentUdi { get; set; } = string.Empty;
    public bool IsPresent { get; set; }
    public bool InUse { get; set; }
    public int Quantity { get; set; }
    public ArticleQuantityUnit UnitOfMeasure { get; set; }
    public int FillPercentage { get; set; }
    public ReagentPositionStatus Status { get; set; }
    public string? Name { get; set; }
    public string? LotNumber { get; set; }
    public DateTimeOffset? ExpiryDate { get; set; }
    public bool UseRemainingQuantity { get; set; }
}

public enum BottlePositionSize
{
    Small,
    Large
}