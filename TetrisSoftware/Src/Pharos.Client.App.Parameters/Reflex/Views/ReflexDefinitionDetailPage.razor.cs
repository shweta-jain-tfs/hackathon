using System.ComponentModel;
using Pharos.Client.App.Parameters.Reflex.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Validation;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Reflex.Views;

public partial class ReflexDefinitionDetailPage : DetailsPageBase<ReflexDefinitionDetailsViewModel, ReflexDefinition>
{
    private HeaderView? _headerViewReference;
    private TestListView? _testsViewReference;
    private PhValidationContext? _validation;

    public ReflexDefinitionDetailPage(
        ReflexDefinitionDetailsPageViewModel pageViewModel,
        IStringLocalizer<Translations> loc,
        NavigationManager navigation,
        IModalService modal,
        IToasterService toasterService,
        IExceptionHandlerService exceptionHandler) : base(pageViewModel, loc, navigation, modal, toasterService, exceptionHandler)
    {
    }

    protected override string ParentPage => PageRoute.Parameters.Reflex;

    protected override string DetailsPage(int id)
    {
        return PageRoute.Parameters.ReflexDefinitionDetails.DetailsPage(id);
    }

    protected override void OnDiscardChanges()
    {
        _headerViewReference?.DiscardChanges();
        _testsViewReference?.DiscardChanges();
    }

    protected override async Task<bool> SaveChanges(bool invokedFromNavigationLock)
    {
        if (DetailViewModel is null)
        {
            return false;
        }

        _validation?.Validate();

        if (!IsValid())
        {
            ShowValidationMessage();
            return false;
        }

        return await SaveChanges(DetailViewModel.Model, DetailViewModel.Model.Id, invokedFromNavigationLock);
    }

    private bool IsValid()
    {
        // Check basic requirements
        var basicRequirements = DetailViewModel is
        {
            DefinitionName.Length: > 0,
            ReflexConditionViewModel.IsCompleteCondition: true,
            IsUniqueDefinitionName: true
        };

        // Check FluentValidation rules
        var validationPassed = _validation?.IsValid() ?? false;

        return basicRequirements && validationPassed;
    }

    // Called when user selects a test in the modal
    // Notifies validation context that TestName changed so validation error clears
    private void OnTestSelected()
    {
        // Safely check dependencies exist before notifying
        if (DetailViewModel?.ReflexConditionViewModel != null)
        {
            _validation?.NotifyFieldChanged(() => DetailViewModel.ReflexConditionViewModel.TestName);
        }

        StateHasChanged();
    }

    private void ShowValidationMessage()
    {
        //If any fields are invalid do not show the modal. Rely on the fields guiding the user.
        if (!(_validation?.IsValid() ?? false))
        {
            return;
        }


        if (DetailViewModel is { IsUniqueDefinitionName: false })
        {
            ShowValidationMessage(Loc["ReflexDetailPage_DefinitionNameMustBeUnique"]);
        }
    }

    private void ShowValidationMessage(string message)
    {
        var opt = new ModalOptions
        {
            Title = Loc["ReflexDetailPage_SaveNotPossible"],
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small,
            Status = ModalStatus.Info,
        };
        var closeButtonText = Loc["ReflexDetailPage_CloseButtonText"];
        Modal.ShowInformation(message, closeButtonText, opt, true);
    }

    protected override void Disposing()
    {
        if (DetailViewModel is not null)
        {
            DetailViewModel.PropertyChanged -= DetailViewModelOnPropertyChanged;
        }
    }

    private void DetailViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ReflexDefinitionDetailsViewModel.IsModified) or nameof(ReflexDefinitionDetailsViewModel.DefinitionName))
        {
            StateHasChanged();
        }
    }

    protected override List<BreadcrumbItem> GetInitialBreadcrumbs()
    {
        return
        [
            new BreadcrumbItem(Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
            new BreadcrumbItem(Loc["NavMenu_Reflex"], ParentPage),
            new BreadcrumbItem("...", ""),
        ];
    }

    protected override void AddBreadcrumbForViewModel()
    {
        var breadcrumbItem = DetailViewModel!.IsNewEntity ? new BreadcrumbItem(Loc["ReflexDetailPage_NewReflexDefinition"], "") : new BreadcrumbItem(DetailViewModel!.DefinitionName ?? "", "");
        Breadcrumbs[^1] = breadcrumbItem;
    }

    protected override void SubscribeToViewModel()
    {
        DetailViewModel!.PropertyChanged += DetailViewModelOnPropertyChanged;
    }
}