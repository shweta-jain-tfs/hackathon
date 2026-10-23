using System.ComponentModel;
using Pharos.Client.App.Parameters.QualityControl.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.QualityControl.Views;

public partial class PhadiaQualityControlDetailsPage : DetailsPageBase<PhadiaQualityControlDetailViewModel, PhadiaQualityControlDetail>
{
    public PhadiaQualityControlDetailsPage(
        PhadiaQualityControlDetailsPageViewModel pageViewModel,
        IStringLocalizer<Translations> loc,
        NavigationManager navigation,
        IModalService modal,
        IToasterService toasterService,
        IExceptionHandlerService exceptionHandler) : base(pageViewModel, loc, navigation, modal, toasterService, exceptionHandler)
    {
    }

    [Inject] private IInstrumentDilutionService InstrumentDilutionService { get; set; } = null!;

    protected override string ParentPage => PageRoute.Parameters.QualityControls;

    protected override string DetailsPage(int id)
    {
        return PageRoute.Parameters.PhadiaQualityControlDetails.DetailsPage(id);
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
        var breadcrumbItem = new BreadcrumbItem(DetailViewModel!.Model.Name ?? "", "");
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

        return await SaveChanges(DetailViewModel.Model, DetailViewModel.Model.Id, invokedFromNavigationLock);
    }

    protected override void Disposing()
    {
        if (DetailViewModel is not null)
        {
            DetailViewModel.PropertyChanged -= DetailViewModelOnPropertyChanged;
        }
    }

    private async Task EditTest(PhadiaQualityControlTest test)
    {
        var viewModel = new EditQualityControlTestViewModel(test.Id,
            test.ApplyAcceptanceRange,
            test.NumberOfTests,
            test.UseAcceptanceRange,
            test.Replicates,
            test.ReplicateCvLimit,
            test.UseLotQualification,
            test.InstrumentDilutionFactor,
            test.MaxCv,
            test.NumberOfSd,
            test.PreDilutionFactor,
            test.QualityControlType,
            InstrumentDilutionService);

        var parameters = new ModalParameters { { nameof(EditQualityControlTestModalView.ViewModel), viewModel } };
        var model = Modal.Show<EditQualityControlTestModalView>(new ModalOptions
        {
            Title = $"{Loc["EditQualityControlTestModalView_EditQcTest"]} - {test.MethodName} : {test.TestName}",
            ModalHeight = ModalSize.XLarge,
            ModalWidth = ModalSize.Large
        }, parameters);
        var result = await model.Result;

        if (!result.Canceled)
        {
            DetailViewModel!.UpdateTest(viewModel);
        }
    }
}