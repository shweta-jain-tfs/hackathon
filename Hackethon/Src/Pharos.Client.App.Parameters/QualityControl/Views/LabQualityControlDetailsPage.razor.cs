using System.ComponentModel;
using Pharos.Client.App.Parameters.QualityControl.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Validation;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.QualityControl.Views;

public partial class LabQualityControlDetailsPage : DetailsPageBase<LabQualityControlDetailViewModel, LabQualityControlDetail>
{
    private PhSimpleValidationContext? _validation;

    public LabQualityControlDetailsPage(
        LabQualityControlDetailsPageViewModel pageViewModel,
        IStringLocalizer<Translations> loc,
        NavigationManager navigation,
        IModalService modal,
        IToasterService toasterService,
        IExceptionHandlerService exceptionHandler) : base(pageViewModel, loc, navigation, modal, toasterService, exceptionHandler)
    {
    }

    protected override string ParentPage => PageRoute.Parameters.QualityControls;

    protected override string DetailsPage(int id)
    {
        return PageRoute.Parameters.LabQualityControlDetails.DetailsPage(id);
    }

    protected override List<BreadcrumbItem> GetInitialBreadcrumbs()
    {
        return
        [
            new BreadcrumbItem(Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
            new BreadcrumbItem(Loc["NavMenu_QualityControls"], ParentPage)
        ];
    }

    protected override void AddBreadcrumbForViewModel()
    {
        var breadcrumbItem = DetailViewModel!.IsNewEntity ? new BreadcrumbItem(Loc["LabQualityControlDetailsPage_NewQc"], "") : new BreadcrumbItem(DetailViewModel.Model.Name ?? "", "");
        Breadcrumbs.Add(breadcrumbItem);
    }

    protected override void SubscribeToViewModel()
    {
        DetailViewModel!.PropertyChanged += DetailViewModelOnPropertyChanged;
    }

    private void DetailViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LabQualityControlDetailViewModel.IsModified))
        {
            StateHasChanged();
        }
    }

    protected override async Task<bool> SaveChanges(bool invokedFromNavigationLock)
    {
        if (DetailViewModel is null)
        {
            return false;
        }

        _validation?.Validate();

        if (!(_validation?.IsValid() ?? false))
        {
            return false;
        }

        if (DetailViewModel.Tests.Count == 0)
        {
            ShowValidationMessage(Loc["LabQualityControlDetailsPage_AtLeastOneTest"]);
            return false;
        }

        return await SaveChanges(DetailViewModel.Model, DetailViewModel.Model.Id, invokedFromNavigationLock);
    }

    private void ShowValidationMessage(string message)
    {
        var opt = new ModalOptions
        {
            Title = Loc["LabQualityControlDetailsPage_SaveNotPossible"],
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small,
            Status = ModalStatus.Info
        };
        var closeButtonText = Loc["LabQualityControlDetailsPage_CloseButtonText"];
        Modal.ShowInformation(message, closeButtonText, opt, true);
    }

    protected override void Disposing()
    {
        if (DetailViewModel is not null)
        {
            DetailViewModel.PropertyChanged -= DetailViewModelOnPropertyChanged;
        }
    }

    private async Task EditBarcodeFormats()
    {
        var modal = Modal.Show<BarcodeFormatsListModalView>(new ModalOptions
        {
            Title = Loc["BarcodeFormatsListModal_ListTitle"],
            ModalHeight = ModalSize.XLarge,
            ModalWidth = ModalSize.Small
        }, new ModalParameters());

        await modal.Result;
    }
}