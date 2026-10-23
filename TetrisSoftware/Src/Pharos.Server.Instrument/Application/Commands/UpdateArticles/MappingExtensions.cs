using Pharos.Server.Application.Settings.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Tetris.Protocol.Instrument.Articles;

namespace Pharos.Server.Instrument.Application.Commands.UpdateArticles;

internal static class MappingExtensions
{
    public static List<TDest> FilterAndMap<TDest>(this IEnumerable<Article> source, ArticleType articleType, Func<Article, TDest> mapper)
    {
        return source.Where(x => x.ArticleType == articleType).Select(mapper).ToList();
    }

    public static EnumWasteType ToEnumWasteType(this WasteType source)
    {
        return source switch
        {
            WasteType.Biohazard => EnumWasteType.BiohazardSolidWaste,
            WasteType.Recyclable => EnumWasteType.RecyclableSolidWaste,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}
