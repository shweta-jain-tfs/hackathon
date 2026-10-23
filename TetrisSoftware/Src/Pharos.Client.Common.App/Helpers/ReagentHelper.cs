using Pharos.Common.Web.Components.Reagent;

namespace Pharos.Client.Common.App.Helpers;

public static class ReagentHelper
{
    public static ReagentIndicatorShape GetShape(ArticleIndicatorShape shape)
    {
        return shape switch
        {
            ArticleIndicatorShape.None => ReagentIndicatorShape.None,
            ArticleIndicatorShape.Round => ReagentIndicatorShape.Round,
            ArticleIndicatorShape.Rectangular => ReagentIndicatorShape.Rectangular,
            ArticleIndicatorShape.Slanted => ReagentIndicatorShape.Slanted,
            _ => ReagentIndicatorShape.None
        };
    }
}
