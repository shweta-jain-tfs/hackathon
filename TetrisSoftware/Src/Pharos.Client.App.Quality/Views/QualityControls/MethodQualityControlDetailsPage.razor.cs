using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.App.Quality.Views.QualityControls.Helpers;
using Pharos.Client.Common.App;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Breadcrumb;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Quality.Views.QualityControls;

public partial class MethodQualityControlDetailsPage : DetailsPageBase<MethodQualityControlDetailsViewModel, QualityControl>
{
    private readonly IQualityControlTubeNotificationProvider _tubeNotifications;

    public MethodQualityControlDetailsPage(
        MethodQualityControlDetailsPageViewModel pageViewModel,
        IQualityControlTubeNotificationProvider tubeNotifications,
        IStringLocalizer<Translations> loc,
        NavigationManager navigation,
        IModalService modal,
        IToasterService toasterService,
        IExceptionHandlerService exceptionHandler) : base(pageViewModel, loc, navigation, modal, toasterService, exceptionHandler)
    {
        _tubeNotifications = tubeNotifications;
    }

    protected override string ParentPage => PageRoute.Quality.Default;

    protected override string DetailsPage(int id)
    {
        return $"{PageRoute.Quality.MethodQualityControl.Default}/{id}";
    }

    protected override List<BreadcrumbItem> GetInitialBreadcrumbs()
    {
        return
        [
            new BreadcrumbItem(Loc["NavMenu_Quality"], PageRoute.Quality.Default)
        ];
    }

    protected override void AddBreadcrumbForViewModel()
    {
        Breadcrumbs.Add(new BreadcrumbItem($"{DetailViewModel!.Model.MethodName} : {DetailViewModel.Model.Name} : {DetailViewModel.Model.TestName}", ""));
    }

    protected override void SubscribeToViewModel()
    {
        _tubeNotifications.TubeUpdated -= OnTubeUpdated;
        _tubeNotifications.TubeUpdated += OnTubeUpdated;
    }

    protected override Task<bool> SaveChanges(bool invokedFromNavigationLock)
    {
        return Task.FromResult(true);
    }

    protected override void Disposing()
    {
        _tubeNotifications.TubeUpdated -= OnTubeUpdated;
    }

    private async void OnTubeUpdated(object? sender, string tubeId)
    {
        if (DetailViewModel is not { IsQcInTube: true })
        {
            return;
        }

        if (DetailViewModel.Model.Tubes.Any(x => x.Id == tubeId))
        {
            await DiscardChanges();
            StateHasChanged();
        }
    }

    private string QualityControlTypeString()
    {
        if (DetailViewModel is null)
        {
            return string.Empty;
        }

        return MethodQualityControlHelper.QualityControlTypeToString(DetailViewModel.Model.IsUserDefined, Loc);
    }
}