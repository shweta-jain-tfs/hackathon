using Pharos.Client.Common.App.ViewModels;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.Common.App.Views;

public partial class ToasterView : IDisposable
{
    [Inject] private ToasterViewModel ViewModel { get; set; } = null!;

    private string? _toastMessage;
    private bool _toastShow;
    private ToastType _toastType;

    public void Dispose()
    {
        ViewModel.OnShowToast -= OnShowToast;

        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        await ViewModel.LoadAsync();
        ViewModel.OnShowToast += OnShowToast;
    }

    private void OnShowToast(ToastType type, string message)
    {
        // Show the toaster.
        _toastType = type;
        _toastMessage = message;
        _toastShow = true;
        StateHasChanged();
    }

    /// <summary>
    /// Have to be able to set show property back to false, when toaster is closed.
    /// </summary>
    /// <param name="show"></param>
    private void OnToastShowChanged(bool show)
    {
        _toastShow = show;
    }
}
