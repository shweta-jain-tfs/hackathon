using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;

namespace Pharos.Server.LoadList.Domain;

public class ArticleUsage
{
    [MaxLength(Constants.InstrumentSerialNumberLength)]
    public string InstrumentNumber { get; set; } = default!;
    [MaxLength(Constants.ArticleNumberLength)]
    public string ArticleIdentity { get; set; } = default!;
    [MaxLength(Constants.LotNumberLength)]
    public string LotNumber { get; set; } = default!;
    [MaxLength(Article.FullNameLength)]
    public string? FullName { get; set; } = default!;
    [MaxLength(Article.FriendlyNameLength)]
    public string? FriendlyName { get; set; } = default!;
    public List<string?> Texts { get; set; } = default!;
    public int Quantity { get; set; }
    public ArticleIndicatorShape Shape { get; set; }
    public List<string?> Colors { get; set; } = default!;
}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.