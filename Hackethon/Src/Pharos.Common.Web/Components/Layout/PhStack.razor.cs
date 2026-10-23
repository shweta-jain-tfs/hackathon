using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Layout;

public partial class PhStack
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public StackGap Gap { get; set; } = StackGap.Gutter;

    protected string CssClasses => new CssBuilder("ph-stack-layout")
        .AddClass("ph-stack-layout--gap-xs", Gap == StackGap.Xs)
        .AddClass("ph-stack-layout--gap-md", Gap == StackGap.Md)
        .AddClass("ph-stack-layout--gap-lg", Gap == StackGap.Lg)
        .AddClass("ph-stack-layout--gap-xxl", Gap == StackGap.Xxl)
        .Build();

}