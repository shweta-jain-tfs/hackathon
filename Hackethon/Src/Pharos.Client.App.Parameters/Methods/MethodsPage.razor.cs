using Pharos.Client.App.Parameters.Methods.ViewModels;
using Pharos.Client.App.Parameters.Methods.Views;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Methods;

public partial class MethodsPage : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private MethodsPageViewModel ViewModel { get; set; } = default!;
    [Inject] private IParametersNotificationProvider NotificationProvider { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;

    protected List<BreadcrumbItem> Breadcrumbs =>
    [
        new BreadcrumbItem(Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
        new BreadcrumbItem(Loc["NavMenu_Methods"], PageRoute.Parameters.Methods)
    ];

    public void Dispose()
    {
        NotificationProvider.ParameterUpdateFinished -= OnParameterUpdateFinished;
        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        NotificationProvider.ParameterUpdateFinished += OnParameterUpdateFinished;
    }

    private void OnParameterUpdateFinished(object? sender, bool e)
    {
        StateHasChanged();
    }

    private async Task OpenSpecializedMethodsAndTests()
    {
        var modal = ModalService.Show<SpecializedMethodsAndTestsView>(new ModalOptions
        {
            Title = Loc["SpecializedMethodsAndTestsModalView_Title"],
            ModalHeight = ModalSize.Medium,
            ModalWidth = ModalSize.Medium
        });

        var result = await modal.Result;
        if (!result.Canceled)
        {
            StateHasChanged();
        }
    }
}
