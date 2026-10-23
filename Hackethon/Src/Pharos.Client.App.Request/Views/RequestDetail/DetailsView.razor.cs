using Pharos.Client.App.Request.Models;
using Pharos.Client.App.Request.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Request.Views.RequestDetail;

public partial class DetailsView : ComponentBase
{
    [Inject] public IAuthorizationService AuthorizationService { get; set; } = null!;
    [Inject] private IModalService ModalService { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private IRequestorDataSource RequestorDataSource { get; set; } = null!;
    [Parameter, EditorRequired] public RequestDetailsViewModel ViewModel { get; set; } = null!;
    [Parameter] public EventCallback OnAddTestsClick { get; set; }
    [Parameter] public EventCallback<RequestTest> OnEditTestClick { get; set; }

    private List<DropDownOption<SampleVolumeCheck>> _sampleVolumeChecks = new();

    private bool NotAuthorized { get; set; } = true;
    private bool DisplayOnly => NotAuthorized || IsRequestStartedOrLater;
    private bool DetailInputsDisabled => !NotAuthorized && ViewModel.IsEmptySampleId;

    private bool PreDilutionDisplayOnly => !ViewModel.CanEditPredilution || DisplayOnly;
    private bool CanEditTubeType => !NotAuthorized && ViewModel.CanEditRequest;
    private bool CanViewRequestInfo { get; set; }
    private bool CanEditRequestInfo { get; set; }
    private bool IsRequestStartedOrLater => ViewModel.RequestState != RequestState.NotStarted;

    protected override async Task OnParametersSetAsync()
    {
        var canManageTestOrders = await AuthorizationService.IsAuthorized(Permissions.ManageTestOrders);
        NotAuthorized = !canManageTestOrders;
        CanViewRequestInfo = ViewModel.UiSettings.RequestSettings.IsRequestorInformationEnabled && await AuthorizationService.IsAuthorized(Permissions.ViewPersonalAndHealthData);
        CanEditRequestInfo = CanViewRequestInfo && canManageTestOrders && !IsRequestStartedOrLater;
    }

    protected override void OnInitialized()
    {
        _sampleVolumeChecks = Enum.GetValues<SampleVolumeCheck>().Select(x => new DropDownOption<SampleVolumeCheck>
        {
            Text = x.ToLocalizedString("RequestDetailPage"),
            Value = x
        }).ToList();
    }

    private async Task ViewRequestorDetails()
    {
        var requestorId = ViewModel.RequestorViewModel.Model.Id;

        if (requestorId == 0)
        {
            return;
        }

        var requestor = await RequestorDataSource.GetRequestorDetails(requestorId);
        var vm = new RequestorDetailsViewModel(requestor);

        var parameters = new ModalParameters
        {
            { nameof(RequestorDetailsView.ViewModel), vm }
        };

        var model = ModalService.Show<RequestorDetailsView>(new ModalOptions { Title = Loc["RequestorDetailsView_Title"], ModalHeight = ModalSize.XLarge, ModalWidth = ModalSize.Large }, parameters);
        await model.Result;

        vm.Dispose();
    }

    private static string FormatRequestor(Requestor? requestor)
    {
        if (requestor is null || requestor.Id == 0)
        {
            return string.Empty;
        }

        var id = requestor.RequestorId;
        var name = requestor.Name;

        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(id))
        {
            return $"{name}, {id}";
        }

        if (!string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return string.Empty;
    }
}