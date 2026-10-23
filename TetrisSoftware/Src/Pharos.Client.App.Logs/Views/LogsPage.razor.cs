using Pharos.Client.Common.App.Interfaces;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Logs.Views;

public partial class LogsPage
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    private const int _errorsTabId = 1;
    private const int _eventsTabId = 2;
    private const int _auditTrailTabId = 3;

    private bool _canViewAuditTrail;
    private int? _errorIdForPanel;
    private int? _respondedErrorIdInPanel;

    private bool IsErrorPanelOpen => _errorIdForPanel.HasValue;

    protected override async Task OnParametersSetAsync()
    {
        _canViewAuditTrail = await AuthorizationService.IsAuthorized(Permissions.ViewAuditTrail);
    }

    private async Task ErrorSelected(int id)
    {
        _errorIdForPanel = id;
        _respondedErrorIdInPanel = null;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ResponseSubmitted()
    {
        if (_errorIdForPanel.HasValue)
        {
            _respondedErrorIdInPanel = _errorIdForPanel.Value;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ErrorPanelCloseClicked()
    {
        _errorIdForPanel = null;
        await InvokeAsync(StateHasChanged);
    }

    private async Task SelectedTabChanged(int selectedTabId)
    {
        if (selectedTabId != _errorsTabId && _errorIdForPanel.HasValue)
        {
            _errorIdForPanel = null;
            await InvokeAsync(StateHasChanged);
        }
    }
}