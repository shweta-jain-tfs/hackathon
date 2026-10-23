using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.BarcodeDetails;
using Pharos.Client.Common.App.Extensions;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Reagents.Views.BarcodeDetails;

public partial class CartridgeReagentDetailsView : IDisposable
{

    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Inject]
    public ReagentDetailsViewModel ViewModel { get; set; } = default!;

    [CascadingParameter]
    public ModalInstance Modal { get; set; } = default!;

    [Parameter]
    public ReagentContentDetail? ReagentContentDetail { get; set; } = default!;

    [Parameter]
    public string? Barcode { get; set; }

    protected override async Task OnInitializedAsync()
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        if (!string.IsNullOrEmpty(Barcode))
        {
            await ViewModel.LoadReagentDetailsAsync(Barcode);
            ReagentContentDetail = ViewModel.ReagentContentDetail;

            // If a toast was shown (no dialog data), close the modal
            if (!string.IsNullOrEmpty(ViewModel.ToastMessage))
            {
                await Modal.CloseAsync(ModalResult.Ok());
            }
        }
    }

    private string GetExpiryDate()
    {
        if (ReagentContentDetail != null && ReagentContentDetail.ExpiryDate.HasValue)
        {
            return ReagentContentDetail.ExpiryDate.Value.ToShortDateString();
        }

        return String.Empty;
    }

    private async Task CloseModal()
    {
        await Modal.CloseAsync(ModalResult.Ok());
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        GC.SuppressFinalize(this);
    }
}
