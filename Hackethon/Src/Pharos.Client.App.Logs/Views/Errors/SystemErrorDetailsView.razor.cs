using Pharos.Client.App.Logs.ViewModels.Errors;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Logs.Views.Errors;

public partial class SystemErrorDetailsView : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private SystemErrorDetailsViewModel ViewModel { get; set; } = default!;

    [CascadingParameter] public ModalInstance? ModalInstance { get; set; }

    [Parameter] public int Id { get; set; }
    [Parameter] public bool IsPanel { get; set; }
    [Parameter] public bool CloseOnResponseSubmitted { get; set; } = true;
    [Parameter] public EventCallback OnResponseSubmitted { get; set; }

    private int? _loadedId;

    private bool HasErrorImage => !string.IsNullOrEmpty(ViewModel.ErrorImagePath);

    /// <summary>
    /// Maximum number of response buttons to show inline before collapsing them into a dropdown.
    /// </summary>
    private const int _maxInlineResponseButtons = 3;

    protected override void OnInitialized()
    {
        ViewModel.OnResponseSubmitted += HandleResponseSubmittedAsync;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedId == Id)
        {
            return;
        }

        _loadedId = Id;
        await ViewModel.LoadAsync(Id);
    }

    private async Task HandleResponseSubmittedAsync()
    {
        if (OnResponseSubmitted.HasDelegate)
        {
            await OnResponseSubmitted.InvokeAsync();
        }

        if (IsPanel)
        {
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (ModalInstance != null && CloseOnResponseSubmitted)
        {
            await ModalInstance.CloseAsync(ModalResult.Ok());
        }
        else
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        ViewModel.OnResponseSubmitted -= HandleResponseSubmittedAsync;
        GC.SuppressFinalize(this);
    }
}
