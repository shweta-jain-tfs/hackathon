using Microsoft.JSInterop;
using Pharos.Common.Web.Extensions;

namespace Pharos.Common.Web.Components.Subnavigation;

public partial class PhSubnavMenu
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? BottomElement { get; set; }
    [Parameter] public bool ScrollActiveItemIntoView { get; set; }

    private ElementReference? _scrollElement;
    private IJSObjectReference? _script;
    private const string _activeItemSelector = ".active";

    protected override async void OnAfterRender(bool firstRender)
    {
        if (firstRender && ScrollActiveItemIntoView)
        {
            _script = await ImportJSAsync();
            await _script.InvokeVoidAsync("scrollActiveItemIntoView", _scrollElement, _activeItemSelector);
        }
    }

    private async void SaveScrollPosition()
    {
        if (_script is not null)
        {
            await _script.InvokeVoidAsync("saveScrollPosition", _scrollElement);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await _script.SafeDisposeAsync();

        GC.SuppressFinalize(this);
    }
}