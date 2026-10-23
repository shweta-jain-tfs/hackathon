using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Reagents.Views.Drawer;
public partial class ReagentLoaderView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Parameter]
    public ReagentLoader Loader { get; set; } = default!;

    [CascadingParameter]
    public bool IsScanningOnGoing { get; set; } = default!;

    [Parameter]
    public EventCallback<ReagentLoader> OnLoaderSelected { get; set; }

    [Parameter]
    public bool IsSelected { get; set; }

    [Parameter]
    public ReagentPositionStatus? LegendSelectedStatus { get; set; }
    
    private string _loaderCss = string.Empty;

    private string _loaderSelectionCss = string.Empty;

    protected override void OnParametersSet()
    {
        SetLoaderCss();
        SetLoaderSelectionCss();
    }

    private void SetLoaderCss()
    {
        _loaderCss = new CssBuilder("reagent-loader")
            .AddClass("reagent-loader--empty", Loader.IsEmpty && !IsScanningOnGoing)
            .AddClass("reagent-loader--filled", !Loader.IsEmpty)
            .AddClass("reagent-loader--scan", IsScanningOnGoing)
            .AddClass("reagent-loader--error", ShowLoaderError())
            .Build();
    }

    private async Task OnLoaderClicked()
    {
        if (IsLoaderSelectable())
        {
            await OnLoaderSelected.InvokeAsync(Loader);
        }
    }

    private void SetLoaderSelectionCss()
    {
        _loaderSelectionCss = new CssBuilder("")
            .AddClass("reagent-loader__selection-frame", IsLoaderSelectable())
            .AddClass("selected", IsLoaderSelected())
            .Build();
    }

    private bool IsLoaderSelectable()
    {
        return !Loader.IsEmpty && !IsScanningOnGoing && Loader.Status != ReagentPositionStatus.ReagentLoaderErrorUnknownObject;
    }

    private bool IsLoaderSelected()
    {
        return IsLoaderSelectable() && IsSelected;
    }

    private bool ShowLoaderError()
    {
        return Loader.Status == ReagentPositionStatus.ReagentLoaderErrorUnknownObject && !IsScanningOnGoing;
    }
}