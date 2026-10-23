namespace Pharos.Common.Web.Utilities;

internal sealed class Debouncer<T> : IAsyncDisposable
{
    private CancellationTokenSource _cts = new();

    public bool IsDebouncing { get; private set; }

    public async Task InvokeAsync(
        T value,
        int delay,
        Func<T, Task> callback)
    {
        await _cts.CancelAsync();
        _cts.Dispose();
        _cts = new CancellationTokenSource();

        var token = _cts.Token;

        IsDebouncing = true;

        try
        {
            await Task.Delay(delay, token);
            await callback(value);
        }
        catch (TaskCanceledException)
        {
            // Expected.
            return;
        }
        finally
        {
            IsDebouncing = false;
        }
    }

    public async Task CancelAsync()
    {
        await _cts.CancelAsync();
        IsDebouncing = false;
    }

    public async ValueTask DisposeAsync()
    {
        await CancelAsync();
        _cts.Dispose();
    }
}