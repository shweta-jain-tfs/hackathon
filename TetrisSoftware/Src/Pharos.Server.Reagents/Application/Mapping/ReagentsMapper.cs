using System.Drawing;
using Google.Protobuf.WellKnownTypes;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Reagents.Application.Queries;
using Pharos.Server.Reagents.Domain;
using Riok.Mapperly.Abstractions;
using Tetris.Protocol.Common;
using LotInformation = Pharos.Server.Application.Reagents.LotInformation;

namespace Pharos.Server.Reagents.Application.Mapping;

[Mapper]
internal static partial class ReagentsMapper
{
    [MapProperty(nameof(ReagentContentDto.LotExpiryDate), nameof(ReagentCarrierContent.LotExpiryDate), Use = nameof(ToTimestampUtc))]
    [MapProperty(nameof(ReagentContentDto.CarrierUdi), nameof(ReagentCarrierContent.ReagentUdiBarcode), Use = nameof(ToNonNullString))]
    [MapProperty(nameof(ReagentContentDto.SerialNumber), nameof(ReagentCarrierContent.SerialNumber), Use = nameof(ToNonNullString))]
    [MapperIgnoreTarget(nameof(ReagentCarrierContent.LoadTimestamp))]
    public static partial ReagentCarrierContent ToReagentCarrierContent(this ReagentContentDto source);

    [MapProperty(nameof(@ReagentContentCellDto.OpenTimestamp), nameof(@ReagentCarrierContentCell.OpenTimestamp), Use = nameof(ToTimestampUtcOrMinValue))]
    public static partial ReagentCarrierContentCell ToReagentCarrierContentCell(this ReagentContentCellDto source);

    [MapperIgnoreSource(nameof(Article.DomainEvents))]
    public static partial Article ToArticle(this Article source);

    [MapperIgnoreTarget(nameof(CartridgeCellIdentity.Article))]
    private static partial CartridgeCellIdentity ToCartridgeCellIdentity(CartridgeCellIdentity source);

    [MapperIgnoreTarget(nameof(Lot.ArticleType))]
    [MapperIgnoreTarget(nameof(Lot.Id))]
    [MapperIgnoreTarget(nameof(Lot.DomainEvents))]
    [MapperIgnoreTarget(nameof(Lot.Reagents))]
    public static partial Lot ToLot(this LotInformation source);

    [MapProperty(nameof(@ReagentInfo.Article.Colors), nameof(@ReagentContentDetailDto.Colors))]
    [MapProperty(nameof(@ReagentInfo.Article.Texts), nameof(@ReagentContentDetailDto.Texts))]
    [MapProperty(nameof(@ReagentInfo.Article.Shape), nameof(@ReagentContentDetailDto.Shape))]
    [MapProperty(nameof(@ReagentInfo.Article.FriendlyName), nameof(@ReagentContentDetailDto.Name))]
    [MapProperty(nameof(@ReagentInfo.Article.ArticleType), nameof(@ReagentContentDetailDto.Type))]
    [MapProperty(nameof(@ReagentInfo.Article.Identity), nameof(@ReagentContentDetailDto.Test))]
    [MapProperty(nameof(@ReagentInfo.Article.Quantity), nameof(@ReagentContentDetailDto.Quantity))]
    [MapProperty(nameof(@ReagentInfo.Article.UnitOfMeasure), nameof(@ReagentContentDetailDto.UnitOfMeasure))]
    [MapProperty(nameof(@ReagentInfo.Lot.LotNumber), nameof(@ReagentContentDetailDto.LotNumber))]
    [MapProperty(nameof(@ReagentInfo.Lot.ExpiryDate), nameof(@ReagentContentDetailDto.ExpiryDate))]
    [MapperIgnoreTarget(nameof(ReagentContentDetailDto.Size))]
    public static partial ReagentContentDetailDto ToReagentContentDetailDto(this ReagentInfo source);

    [MapEnumValue(ArticleType.None, EnumReagentType.UnspecifiedReagentType)]
    [MapEnumValue(ArticleType.EliaCalibratorWellCarrier, EnumReagentType.EliaCalibrationWellCarrier)]
    [MapEnumValue(ArticleType.FluoroCVial, EnumReagentType.FluoroC)]
    [MapEnumValue(ArticleType.MaintenanceConcentrateBottle, EnumReagentType.MaintenanceConcentrate)]
    [MapEnumValue(ArticleType.StopSolutionBottle, EnumReagentType.StopSolution)]
    [MapEnumValue(ArticleType.WashAdditiveBottle, EnumReagentType.WashAdditive)]
    [MapEnumValue(ArticleType.WashConcentrateBottle, EnumReagentType.WashConcentrate)]
    [MapperIgnoreSourceValue(ArticleType.DilutionPlate)]
    [MapperIgnoreTargetValue(EnumReagentType.DilutionPlate)]
    public static partial EnumReagentType ToEnumReagentType(this ArticleType source);

    [MapEnumValue(EnumReagentType.UnspecifiedReagentType, ArticleType.None)]
    [MapEnumValue(EnumReagentType.EliaCalibrationWellCarrier, ArticleType.EliaCalibratorWellCarrier)]
    [MapEnumValue(EnumReagentType.FluoroC, ArticleType.FluoroCVial)]
    [MapEnumValue(EnumReagentType.MaintenanceConcentrate, ArticleType.MaintenanceConcentrateBottle)]
    [MapEnumValue(EnumReagentType.StopSolution, ArticleType.StopSolutionBottle)]
    [MapEnumValue(EnumReagentType.WashAdditive, ArticleType.WashAdditiveBottle)]
    [MapEnumValue(EnumReagentType.WashConcentrate, ArticleType.WashConcentrateBottle)]
    [MapperIgnoreSourceValue(EnumReagentType.DilutionPlate)]
    [MapperIgnoreTargetValue(ArticleType.DilutionPlate)]
    public static partial ArticleType ToArticleType(this EnumReagentType source);

    private static string ToGrpcString(string source)
    {
        return string.IsNullOrEmpty(source) ? string.Empty : source;
    }

    private static string ToNonNullString(string? source)
    {
        return source ?? string.Empty;
    }

    private static Timestamp ToTimestampUtc(DateTimeOffset source)
    {
        return Timestamp.FromDateTimeOffset(source.ToUniversalTime());
    }

    private static Timestamp ToTimestampUtcOrMinValue(DateTimeOffset? source)
    {
        return Timestamp.FromDateTimeOffset((source ?? DateTimeOffset.MinValue).ToUniversalTime());
    }

    private static Color? CloneColor(Color? source)
    {
        return source == null ? null : Color.FromArgb(source.Value.ToArgb());
    }

    private static string? ConvertColorToString(Color? source)
    {
        if (source == null)
        {
            return null;
        }
        return ColorTranslator.ToHtml(source.Value);
    }
}
