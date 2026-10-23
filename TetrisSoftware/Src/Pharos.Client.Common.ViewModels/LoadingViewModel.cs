using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.Common.ViewModels;

/// <summary>
/// View model base for an async load lifecycle with busy state and reload gating.
/// </summary>
/// <remarks>
/// Pick this when the view model needs:
/// - A standard async load entry point
/// - Busy state during load
/// - A NeedsReload flag to avoid reloading when nothing changed
///
/// Alternatives:
/// <see cref="ViewModelBase"/> (no load lifecycle).
/// </remarks>
public abstract class LoadingViewModel : ViewModelBase, ILoadingViewModel
{
    public bool IsBusy
    {
        get;
        protected set => SetField(ref field, value);
    }

    public bool NeedsReload
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public async Task LoadAsync()
    {
        if (!NeedsReload)
        {
            return;
        }
        CheckIsBusy();
        IsBusy = true;
        try
        {
            await DoLoadAsync();
            NeedsReload = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected void CheckIsBusy()
    {
        if (IsBusy)
        {
            throw new InvalidOperationException("Concurrent updates of the view-model are not allowed.");
        }
    }

    protected abstract Task DoLoadAsync();
}