using Pharos.Common.Web.Services;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.TabControl;

public partial class PhTabControl : ComponentBase
{
    [Inject] private IComponentStateService ComponentStateService { get; set; } = default!;

    [Parameter]
    public required RenderFragment ChildContent { get; set; }

    /// <summary>
    /// <see cref="PhTabPage.TabId"/> of the tab to select by default.
    /// If not set, the first tab is selected until the user changes tab.
    /// </summary>
    [Parameter] public int? DefaultTab { get; set; }

    /// <summary>
    /// Unique identifier for persisting this tab controls's state across page navigation.
    /// When set, the tab control automatically saves and restores the selected tab.
    /// Set to null or empty to disable state persistence.
    /// </summary>
    /// <remarks>
    /// This property has a default value since most tab controls should have their
    /// state persisted and there's usually only one tab control per page.
    /// </remarks>
    [Parameter] public string? StateID { get; set; } = _defaultStateID;

    /// <summary>
    /// Disable all tabs in the tab control.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Possibility to change the styling.
    /// </summary>
    [Parameter] public TabControlStyle Style { get; set; } = TabControlStyle.Primary;

    /// <summary>
    /// Callback fired whenever the selected tab changes.
    /// </summary>
    [Parameter] public EventCallback<int> OnSelectedTabChanged { get; set; }

    public PhTabPage? SelectedPage { get; set; }

    protected string CssClasses => new CssBuilder("ph-tab-control")
        .AddClass($"ph-tab-control--style-secondary", Style == TabControlStyle.Secondary)
        .Build();

    private string StateKey => StateID == _defaultStateID ? "tabcontrol" : $"tabcontrol_{StateID}";
    private bool DisableState => string.IsNullOrWhiteSpace(StateID);

    private const string _defaultStateID = "__default__";
    private readonly List<PhTabPage> _pages = new();
    private int? _defaultTab;
    private bool _initialSelectionApplied;

    protected override async Task OnParametersSetAsync()
    {
        if (DefaultTab.HasValue && DefaultTab.Value != _defaultTab)
        {
            _defaultTab = DefaultTab.Value;

            // If initial selection has been applied, handle DefaultTab change immediately
            if (_initialSelectionApplied && _pages.Count != 0)
            {
                await HandleDefaultTabChange(_defaultTab.Value);
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Assign effective IDs to all tabs
            AssignEffectiveTabIds();
        }

        if (!_initialSelectionApplied)
        {
            await ApplyInitialTabSelection();
            _initialSelectionApplied = true;
        }
    }

    /// <summary>
    /// Assigns effective tab IDs: uses explicit TabId or generates 100 + index
    /// </summary>
    private void AssignEffectiveTabIds()
    {
        for (var i = 0; i < _pages.Count; i++)
        {
            var page = _pages[i];
            page.EffectiveTabId = page.TabId ?? (100 + i);
        }
    }

    internal void AddPage(PhTabPage tabPage)
    {
        _pages.Add(tabPage);
    }

    private async Task ApplyInitialTabSelection()
    {
        PhTabPage? tabToSelect = null;

        // Priority 1: Try to restore from saved state
        if (!DisableState)
        {
            tabToSelect = await TryGetSelectedTabFromState();
        }

        // Priority 2: Use default tab if specified
        if (tabToSelect == null && _defaultTab.HasValue)
        {
            tabToSelect = _pages.FirstOrDefault(p => p.EffectiveTabId == _defaultTab.Value);
        }

        // Priority 3: Use first available tab
        tabToSelect ??= _pages.FirstOrDefault();

        if (tabToSelect != null)
        {
            SelectedPage = tabToSelect;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles DefaultTab parameter changes after initial selection has been applied
    /// </summary>
    private async Task HandleDefaultTabChange(int newDefaultTabId)
    {
        var newDefaultTab = _pages.FirstOrDefault(p => p.EffectiveTabId == newDefaultTabId);

        if (newDefaultTab != null && SelectedPage != newDefaultTab)
        {
            await ActivatePage(newDefaultTab);
            StateHasChanged();
        }
    }

    private async Task ActivatePage(PhTabPage page)
    {
        if (SelectedPage == page)
        {
            return;
        }

        SelectedPage = page;
        await SaveSelectedTabToState(page.EffectiveTabId);

        if (OnSelectedTabChanged.HasDelegate)
        {
            await OnSelectedTabChanged.InvokeAsync(page.EffectiveTabId);
        }
    }

    public async Task SetSelectedPage(int tabId)
    {
        var page = _pages.FirstOrDefault(x => x?.EffectiveTabId == tabId);

        if (page != null && SelectedPage != page)
        {
            await ActivatePage(page);
            StateHasChanged();
        }
    }

    /// <summary>
    /// State object for persisting tab selection
    /// </summary>
    private class TabControlState
    {
        public int SelectedTabId { get; set; }
    }

    private async Task<PhTabPage?> TryGetSelectedTabFromState()
    {
        if (DisableState)
        {
            return null;
        }

        try
        {
            var savedState = await ComponentStateService.GetStateAsync<TabControlState>(StateKey);

            if (savedState != null)
            {
                return _pages.FirstOrDefault(p => p.EffectiveTabId == savedState.SelectedTabId);
            }
        }
        catch
        {
            // If state loading fails, continue without saved state
        }

        return null;
    }

    private async Task SaveSelectedTabToState(int tabId)
    {
        if (DisableState)
        {
            return;
        }

        try
        {
            var state = new TabControlState
            {
                SelectedTabId = tabId
            };

            await ComponentStateService.SetStateAsync(StateKey, state);
        }
        catch
        {
            // If state saving fails, continue silently
        }
    }
}