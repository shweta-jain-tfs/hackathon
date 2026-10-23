using Pharos.Client.App.Request.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.Status;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Request.Views.RequestDetail;
public partial class HeaderView : ComponentBase
{
    [Inject] public IAuthorizationService AuthorizationService { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Parameter, EditorRequired] public RequestDetailsViewModel ViewModel { get; set; } = default!;

    private PhTextBox? _sampleIdFieldRef;
    private bool _notAuthorized = true;

    protected override async Task OnParametersSetAsync()
    {
        _notAuthorized = !await IsAuthorized();
    }


    /// <summary>
    /// Set focus on sample id when new entity.
    /// </summary>
    /// <returns></returns>
    private async Task SetFocusWhenNewEntity()
    {
        if (ViewModel.IsNewEntity && _sampleIdFieldRef is not null)
        {
            await _sampleIdFieldRef.FocusAsync();
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _ = SetFocusWhenNewEntity();
        }
    }

    public void DiscardChanges()
    {
        _ = SetFocusWhenNewEntity();
    }

    private async Task<bool> IsAuthorized()
    {
        return await AuthorizationService.IsAuthorized(Permissions.ManageTestOrders);
    }
    
    private static StatusType GetStatusType(RequestState state)
    {
        return state switch
        {
            RequestState.NotStarted => StatusType.WorkflowNotStarted,
            RequestState.Started => StatusType.WorkflowInProgress,
            RequestState.PartlyCompleted => StatusType.WorkflowInProgress,
            RequestState.InReview => StatusType.WorkflowInProgress,
            RequestState.Completed => StatusType.WorkflowCompleted,
            _ => StatusType.None
        };
    }
}