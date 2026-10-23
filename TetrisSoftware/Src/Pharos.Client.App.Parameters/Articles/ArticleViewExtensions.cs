using System.Globalization;

namespace Pharos.Client.App.Parameters.Articles;
internal static class ArticleViewExtensions
{
    public static string GetQuantityText(this PackageItemDetailed item, IStringLocalizer<Translations> loc)
    {
        return string.Format(CultureInfo.CurrentCulture, GetQuantityFormatString(item.UnitOfMeasure, loc), item.Quantity);
    }

    public static string GetQuantityFormatString(ArticleQuantityUnit unit, IStringLocalizer<Translations> loc)
    {
        if (unit == ArticleQuantityUnit.Tests)
        {
            return loc["ArticlesPage_FormatTests"];
        }

        if (unit == ArticleQuantityUnit.Sets)
        {
            return loc["ArticlesPage_FormatSets"];
        }

        if (unit == ArticleQuantityUnit.Milliliters)
        {
            return loc["ArticlesPage_FormatMilliliters"];
        }

        if (unit == ArticleQuantityUnit.Doses)
        {
            return loc["ArticlesPage_FormatDoses"];
        }

        throw new InvalidOperationException($"Unknown value of {nameof(ArticleQuantityUnit)}.");
    }
}
