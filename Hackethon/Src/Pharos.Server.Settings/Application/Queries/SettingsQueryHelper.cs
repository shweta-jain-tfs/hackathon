using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Queries;

internal static class SettingsQueryHelper
{
    public static void ReplaceSubPages(ref Page page)
    {
        for (var i = 0; i < page.Children.Count; i++)
        {
            var child = page.Children[i];
            switch (child)
            {
                case Page subPage:
                    {
                        page.Children[i] = CreatePagePlaceholder(subPage);
                        break;
                    }
                case PageList pageList:
                    {
                        if (pageList.PageStructure == null)
                        {
                            throw new InvalidOperationException($"PageList's pageStructure cannot be null");
                        }

                        pageList.PageStructure = CreatePagePlaceholder(pageList.PageStructure);

                        for (var j = 0; j < pageList.PageEntries.Count; j++)
                        {
                            // Gather options to send to client for dynamic dropdowns to work properly
                            var options = ((Page)pageList.PageEntries[j]).Children.OfType<Option>().ToList();
                            pageList.PageEntries[j] = CreatePagePlaceholder(pageList.PageEntries[j], options);
                        }

                        break;
                    }
                case Group group:
                    {
                        for (var j = 0; j < group.Children.Count; j++)
                        {
                            if (group.Children[j] is Page groupSubPage)
                            {
                                group.Children[j] = CreatePagePlaceholder(groupSubPage);
                            }
                        }

                        break;
                    }
            }
        }
    }

    public static PagePlaceholder CreatePagePlaceholder(ChildPageItem source, List<Option>? options = null)
    {
        return new PagePlaceholder
        {
            Key = source.Key,
            Label = source.Label,
            Url = source.Url,
            Dependency = source.Dependency,
            Options = options ?? []
        };
    }
}