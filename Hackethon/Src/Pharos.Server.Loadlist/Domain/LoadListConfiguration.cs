using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.LoadList.Domain;

public class LoadListConfiguration : BaseAuditableEntity
{
    [MaxLength(Constants.InstrumentSerialNumberLength)]
    public string InstrumentSerial { get; set; } = default!;

    [MaxLength(Article.IdentityLength)]
    public string? Identity { get; set; }

    [MaxLength(Constants.ArticleNumberLength)]
    public string ArticleNumber { get; set; } = default!;

    public int Quantity { get; set; }

    [MaxLength(Article.FriendlyNameLength)]
    public string? FriendlyName { get; set; }

    [MaxLength(Article.FullNameLength)]
    public string? FullName { get; set; }

    [MaxLength(Constants.MethodNameLength)]
    public string? MethodName { get; set; }

    public ArticleType ArticleType { get; set; }

    public ArticleIndicatorShape Shape { get; set; }
    public ArticleQuantityUnit Unit { get; set; } = default!;
    public int RefillLevel { get; set; }
    public int TriggerLevel { get; set; }
    
    /// <summary>
    /// An extra label used to represent the article in the Pharos UI.
    /// For cartridges, this is a method name(printed on the label).
    /// Cartridges can be used in up to 4 assay methods.
    /// </summary>
    [MaxLength(Article.TextFieldLength)]
    public string? Text1 { get; set; }

    /// <summary>
    /// An extra label used to represent the article in the Pharos UI.
    /// For cartridges, this is a method name(printed on the label).
    /// Cartridges can be used in up to 4 assay methods.
    /// </summary>
    [MaxLength(Article.TextFieldLength)]
    public string? Text2 { get; set; }

    /// <summary>
    /// An extra label used to represent the article in the Pharos UI.
    /// For cartridges, this is a method name(printed on the label).
    /// Cartridges can be used in up to 4 assay methods.
    /// </summary>
    [MaxLength(Article.TextFieldLength)]
    public string? Text3 { get; set; }

    /// <summary>
    /// An extra label used to represent the article in the Pharos UI.
    /// For cartridges, this is a method name(printed on the label).
    /// Cartridges can be used in up to 4 assay methods.
    /// </summary>
    [MaxLength(Article.TextFieldLength)]
    public string? Text4 { get; set; }


    /// <summary>
    /// Color of the article indicator in UI.
    /// </summary>
    [MaxLength(7)]
    public string? Color1 { get; set; }

    /// <summary>
    /// Color of the article indicator in UI.
    /// Used together with <see cref="Text2"/>. Not used for carriers.
    /// </summary>
    [MaxLength(7)]
    public string? Color2 { get; set; }

    /// <summary>
    /// Color of the article indicator in UI.
    /// Used together with <see cref="Text3"/>. Not used for carriers.
    /// </summary>
    [MaxLength(7)]
    public string? Color3 { get; set; }

    /// <summary>
    /// Color of the article indicator in UI.
    /// Used together with <see cref="Text4"/>. Not used for carriers.
    /// </summary>
    [MaxLength(7)]
    public string? Color4 { get; set; }

    public List<string?> Texts => [Text1, Text2, Text3, Text4];
    public List<string?> Colors => [Color1, Color2, Color3, Color4];
}