using Pharos.Client.App.Parameters.Reflex.Models;
using Pharos.Client.App.Parameters.Reflex.ViewModels;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Reflex.Views;

public partial class DetailsView : ComponentBase
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IRequestorDataSource RequestorDataSource{ get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;
    [Parameter, EditorRequired] public ReflexDefinitionDetailsViewModel ViewModel { get; set; } = default!;
    [Parameter] public EventCallback OnAddTestsClick { get; set; }
    [Parameter] public EventCallback<ReflexTest> OnEditTestClick { get; set; }

    private bool IsEmptyDefinitionName()
    {
        return string.IsNullOrWhiteSpace(ViewModel.DefinitionName);
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