using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.Common.App.CommonViews.Errors;

public class ErrorContentView : ComponentBase
{
    [Inject] private IModalService Modal { get; set; } = default!;
    [Inject] private IExceptionHandlerService ExceptionHandler { get; set; } = default!;
    [Parameter] public Exception? Exception { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<bool> OnReload { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        await OpenModal();
    }

    private async Task OpenModal()
    {
        if (Exception is not null)
        {
            ExceptionHandler.ShowExceptionMessage(Exception);
        }
        else
        {
            var modal = Modal.Show<ErrorView>(new ModalOptions
            {
                Title = "An unhandled error has occurred",
                CloseOnNavigation = false
            }, new ModalParameters
            {
                { nameof(ErrorView.ErrorMessage), Exception?.Message }
            });

            var result = await modal.Result;

            if (result.Canceled)
            {
                await OnClose.InvokeAsync();
            }
            else if (result.Data is bool data)
            {
                await OnReload.InvokeAsync(data);
            }
            else
            {
                await OnReload.InvokeAsync(false);
            }
        }
    }
}