using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.ViewModels.RequestGenerator;
using Pharos.Client.Common.App.Validation;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.ServiceTools.Views.RequestGenerator;

public partial class GeneratorView : IDisposable
{
    [Inject]
    public GeneratorViewModel ViewModel { get; set; } = null!;

    [Inject]
    private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Inject]
    private IModalService ModalService { get; set; } = default!;

    private PhValidationContext? _validationContext;
    private bool IsValid { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        await ViewModel.LoadAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (!firstRender)
        {
            return;
        }

        _validationContext?.Validate();
        StateHasChanged();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private void OnValidationContextStateChanged(ValidationContextState state)
    {
        IsValid = state.IsValid;
        InvokeAsync(StateHasChanged);
    }

    private bool CanGenerate => IsValid && (ViewModel.GenerateRealTests ? ViewModel.CanGenerate : ViewModel.CanPreview);

    private async Task GenerateClicked()
    {
        if (_validationContext is not null && !_validationContext.Validate())
        {
            return;
        }

        if (ViewModel.GenerateRealTests)
        {
            var options = new ModalOptions
            {
                Title = Loc["RequestGenerator_ConfirmRealTests_Title"],
                ModalWidth = ModalSize.XSmall,
                ModalHeight = ModalSize.XSmall
            };

            var modal = ModalService.ShowConfirm(
                Loc["RequestGenerator_ConfirmRealTests_Message"],
                Loc["Cancel"],
                Loc["OK"],
                options,
                true);

            var result = await modal.Result;

            if (!result.Canceled)
            {
                await ViewModel.GenerateAsync();
            }
        }
        else
        {
            await ViewModel.GeneratePreviewAsync();
        }
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        GC.SuppressFinalize(this);
    }
}
