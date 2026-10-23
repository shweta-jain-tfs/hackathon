using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.ServiceTools.ViewModels;

internal class LoadCmrViewModel(HttpClient httpClient, ICurrentInstrumentViewModel instrumentModel, IExceptionHandlerService exceptionHandler) : LoadingViewModel, IDisposable
{
    private bool _isSubscribedToNotifications;

    public bool IsUploading
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(IsUploading));
            OnPropertyChanged(nameof(CanLoadCmr));
        }
    }

    public bool CanLoadCmr => instrumentModel.State is (InstrumentState.Standby or InstrumentState.AssayCmrFinished or InstrumentState.CmrPrepare) && !IsUploading;

    protected override Task DoLoadAsync()
    {
        SubscribeToNotifications();
        return Task.CompletedTask;
    }

    public async Task<CmrTestOrderResult?> UploadCmrFileAsync(IBrowserFile file)
    {
        IsUploading = true;
        try
        {
            await using var fileStream = file.OpenReadStream(file.Size);
            var client = new CustomMadeRunsClient(httpClient);
            return await client.UploadFileAsync(new FileParameter(fileStream, file.Name));
        }
        catch (Exception ex)
        {
            exceptionHandler.ShowExceptionMessage(ex);
            return null;
        }
        finally
        {
            IsUploading = false;
        }
    }

    private void SubscribeToNotifications()
    {
        if (_isSubscribedToNotifications)
        {
            return;
        }

        instrumentModel.OnCurrentInstrumentChange += OnInstrumentStateChanged;
        _isSubscribedToNotifications = true;
    }

    private void UnsubscribeFromNotifications()
    {
        if (!_isSubscribedToNotifications)
        {
            return;
        }

        instrumentModel.OnCurrentInstrumentChange -= OnInstrumentStateChanged;
    }

    private void OnInstrumentStateChanged()
    {
        OnPropertyChanged(nameof(CanLoadCmr));
    }

    public void Dispose()
    {
        UnsubscribeFromNotifications();
        GC.SuppressFinalize(this);
    }
}
