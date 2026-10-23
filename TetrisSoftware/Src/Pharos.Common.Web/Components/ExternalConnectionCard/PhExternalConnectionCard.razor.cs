using Pharos.Common.Web.Components.Health;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.ExternalConnectionCard;

public partial class PhExternalConnectionCard
{
    [Parameter] public string NameTitle { get; set; } = "";
    [Parameter] public string? Name { get; set; }

    [Parameter] public string StateTitle { get; set; } = "";
    [Parameter] public string? State { get; set; }
    [Parameter] public HealthStatus HealthStatus { get; set; }
    [Parameter] public bool ShowHealthStatus { get; set; }

    [Parameter] public bool ShowExportPortDetail { get; set; }
    [Parameter] public string? ExportPortStateTitle { get; set; }
    [Parameter] public string? ExportPortState { get; set; }
    [Parameter] public HealthStatus ExportPortHealthStatus { get; set; }
    [Parameter] public bool ShowExportPortHealthStatus { get; set; }

    [Parameter] public bool IsExpanded { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }


    private bool _isExpanded;

    protected override void OnInitialized()
    {
        _isExpanded = IsExpanded;
    }

    private void ToggleExpansion()
    {
        _isExpanded = !_isExpanded;
    }

    private string ClassConnectionCard =>
        new CssBuilder("ph-external-connection-card")
            .AddClass("ph-external-connection-card--expanded", _isExpanded)
            .Build();
}