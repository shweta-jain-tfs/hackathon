namespace Pharos.Common.Web.Components.Loader
{
    public class LoaderService
    {
        public event Action<Guid>? OnStart;
        public event Action<Guid>? OnStop;

        public event Action<Guid, string?>? OnStartProgress;
        public event Action<Guid>? OnStopProgress;

        public event Action<Guid, int, int>? OnProgressUpdate;

        public event Action? OnCancel;
        public event Action? OnHideVisual;

        public void Show(Guid callId)
        {
            OnStart?.Invoke(callId);
        }

        public void Hide(Guid callId)
        {
            OnStop?.Invoke(callId);
        }

        public void ShowProgress(Guid callId, string? text)
        {
            OnStartProgress?.Invoke(callId, text);
        }

        public void HideProgress(Guid callId)
        {
            OnStopProgress?.Invoke(callId);
        }

        public void UpdateProgress(Guid callId, int current, int total)
        {
            OnProgressUpdate?.Invoke(callId, current, total);
        }

        public void Cancel()
        {
            OnCancel?.Invoke();
        }

        public void HideVisual()
        {
            OnHideVisual?.Invoke();
        }
    }
}