namespace Pharos.Common.Web.Components.ConditionalIndentContent;
public partial class PhConditionalIndentContent
{
    [Parameter] public bool ShowContent { get; set; }
    [Parameter] public required RenderFragment ConditionalContent { get; set; }
    [Parameter] public required RenderFragment ParentContent { get; set; }
}
