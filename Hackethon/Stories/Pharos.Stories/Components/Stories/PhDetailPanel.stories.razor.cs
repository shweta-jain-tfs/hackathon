using Microsoft.AspNetCore.Components;

namespace Pharos.Stories.Components.Stories;

public class PhDetailPanelStories : ComponentBase
{
    protected bool _isDefaultOpen = true;
    protected bool _isLimiterVariantOpen = true;
    protected bool _isNoLimiterVariantOpen = true;

    protected void OpenDefaultPanel() => _isDefaultOpen = true;
    protected void CloseDefaultPanel() => _isDefaultOpen = false;

    protected void OpenLimiterVariant() => _isLimiterVariantOpen = true;
    protected void CloseLimiterVariant() => _isLimiterVariantOpen = false;

    protected void OpenNoLimiterVariant() => _isNoLimiterVariantOpen = true;
    protected void CloseNoLimiterVariant() => _isNoLimiterVariantOpen = false;
}
