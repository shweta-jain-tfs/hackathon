using System.Drawing;
using Pharos.Server.LoadList.Domain;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.LoadList.Application.Mapping;

[Mapper]
internal static partial class LoadListConfigurationMapper
{
    [MapProperty(nameof(Article.UnitOfMeasure), nameof(LoadListConfiguration.Unit))]
    [MapProperty(nameof(Article.Color1), nameof(LoadListConfiguration.Color1), Use = nameof(MapColorToHtml))]
    [MapProperty(nameof(Article.Color2), nameof(LoadListConfiguration.Color2), Use = nameof(MapColorToHtml))]
    [MapProperty(nameof(Article.Color3), nameof(LoadListConfiguration.Color3), Use = nameof(MapColorToHtml))]
    [MapProperty(nameof(Article.Color4), nameof(LoadListConfiguration.Color4), Use = nameof(MapColorToHtml))]
    [MapperIgnoreTarget(nameof(LoadListConfiguration.Id))]
    [MapperIgnoreTarget(nameof(LoadListConfiguration.Created))]
    [MapperIgnoreTarget(nameof(LoadListConfiguration.CreatedBy))]
    [MapperIgnoreTarget(nameof(LoadListConfiguration.LastModified))]
    [MapperIgnoreTarget(nameof(LoadListConfiguration.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(LoadListConfiguration.InstrumentSerial))]
    [MapperIgnoreTarget(nameof(LoadListConfiguration.Identity))]
    [MapperIgnoreTarget(nameof(LoadListConfiguration.MethodName))]
    [MapperIgnoreTarget(nameof(LoadListConfiguration.RefillLevel))]
    [MapperIgnoreTarget(nameof(LoadListConfiguration.TriggerLevel))]
    [MapperIgnoreTarget(nameof(LoadListConfiguration.Texts))]
    [MapperIgnoreTarget(nameof(LoadListConfiguration.Colors))]
    [MapperIgnoreTarget(nameof(LoadListConfiguration.DomainEvents))]
    private static partial void MapFromArticle(Article source, LoadListConfiguration target);

    public static void UpdateFromArticle(Article source, LoadListConfiguration target)
    {
        MapFromArticle(source, target);
        target.FriendlyName ??= target.Identity;
    }

    private static string? MapColorToHtml(Color? color)
    {
        return color.HasValue ? ColorTranslator.ToHtml(color.Value) : null;
    }
}
