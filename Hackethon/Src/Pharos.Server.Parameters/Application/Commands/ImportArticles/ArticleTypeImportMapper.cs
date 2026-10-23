namespace Pharos.Server.Parameters.Application.Commands.ImportArticles;

internal static class ArticleTypeImportMapper
{
    public static ArticleType Normalize(ArticleType articleType)
    {
        return (int)articleType switch
        {
            -1 => articleType,

            7 => ArticleType.StopSolutionBottle,
            10 => ArticleType.FluoroCVial,
            20 => ArticleType.WashConcentrateBottle,
            21 => ArticleType.WashAdditiveBottle,

            36 => ArticleType.StopSolutionBottle,
            37 => ArticleType.WashConcentrateBottle,
            38 => ArticleType.WashAdditiveBottle,
            39 => ArticleType.FluoroCVial,

            _ => articleType
        };
    }
}
