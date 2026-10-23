using Pharos.Client.App.Request.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Request.Views.RequestDetail;

public partial class TubesView : ComponentBase, IDisposable
{
    private readonly IModalService _modalService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IStringLocalizer<Translations> _localization;
    private bool HasManageTestOrdersPermission { get; set; }
    private bool _canViewSampleTubeImage;
    
    public TubesView(IModalService modalService, IAuthorizationService authorizationService, IStringLocalizer<Translations> localization)
    {
        _modalService = modalService;
        _authorizationService = authorizationService;
        _localization = localization;
    }

    [Parameter, EditorRequired] public RequestDetailsViewModel ViewModel { get; set; } = default!;

    public bool ShowTubeId => ViewModel.UiSettings.RequestSettings.ShowTubeId;

    protected override async Task OnInitializedAsync()
    {
        HasManageTestOrdersPermission = await _authorizationService.IsAuthorized(Permissions.ManageTestOrders);
        _canViewSampleTubeImage = await _authorizationService.IsRndMode() || await _authorizationService.IsServiceRole();
    }

    private async Task ShowImage(SampleTubeHistoryViewModel item)
    {
        if (!_canViewSampleTubeImage || string.IsNullOrEmpty(item.InstrumentNumber) || string.IsNullOrEmpty(item.ImageIdentity))
        {
            return;
        }

        var modal = _modalService.Show<ImageView>(new ModalOptions { Title = _localization["ImageView_Title"] }, new ModalParameters
        {
            { nameof(ImageView.InstrumentNumber), item.InstrumentNumber },
            { nameof(ImageView.ImageId), item.ImageIdentity }
        });
        await modal.Result;
    }

    private async Task UnloadSampleTubes()
    {
        await ViewModel.UnloadSampleTubes();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}