namespace Pharos.Client.Common.ViewModels;

/// <summary>
///     Provides debouncing functionality to delay execution of an action until a specified time has elapsed
///     without new invocations.
/// </summary>
public sealed class Debouncer : IDisposable
{
    private readonly int _delayMilliseconds;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly Lock _lock = new();

    public Debouncer(int delayMilliseconds)
    {
        _delayMilliseconds = delayMilliseconds;
    }

    /// <summary>
    ///     Debounces the execution of the provided action. If called multiple times, only the last call will execute
    ///     after the delay period.
    /// </summary>
    /// <param name="action">The action to execute after the debounce period.</param>
    public void Debounce(Func<CancellationToken, Task> action)
    {
        CancellationTokenSource? cts;
        lock (_lock)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            cts = new CancellationTokenSource();
            _cancellationTokenSource = cts;
        }

        _ = DebounceAsync(action, cts);
    }

    private async Task DebounceAsync(Func<CancellationToken, Task> action, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(_delayMilliseconds, cts.Token).ConfigureAwait(false);

            if (cts.Token.IsCancellationRequested)
            {
                return;
            }

            await action(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when debounce is reset
        }
        catch (Exception)
        {
            // Catch any unexpected exceptions to prevent unobserved task exceptions
        }
        finally
        {
            lock (_lock)
            {
                if (_cancellationTokenSource == cts)
                {
                    _cancellationTokenSource = null;
                }
            }
            cts.Dispose();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
}

/// <summary>
///     Provides debouncing functionality with support for multiple keys, allowing independent debouncing
///     for different identifiers.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify different debounce instances.</typeparam>
public sealed class KeyedDebouncer<TKey> : IDisposable where TKey : notnull
{
    private readonly int _delayMilliseconds;
    private readonly Dictionary<TKey, CancellationTokenSource> _debounceTokens = new();
    private readonly Lock _lock = new();

    public KeyedDebouncer(int delayMilliseconds)
    {
        _delayMilliseconds = delayMilliseconds;
    }

    /// <summary>
    ///     Debounces the execution of the provided action for a specific key. If called multiple times with the same key,
    ///     only the last call will execute after the delay period.
    /// </summary>
    /// <param name="key">The key identifying this debounce instance.</param>
    /// <param name="action">The action to execute after the debounce period.</param>
    public void Debounce(TKey key, Func<CancellationToken, Task> action)
    {
        CancellationTokenSource? cts;
        lock (_lock)
        {
            if (_debounceTokens.TryGetValue(key, out var existing))
            {
                existing.Cancel();
            }

            cts = new CancellationTokenSource();
            _debounceTokens[key] = cts;
        }

        _ = DebounceAsync(key, action, cts);
    }

    private async Task DebounceAsync(TKey key, Func<CancellationToken, Task> action, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(_delayMilliseconds, cts.Token).ConfigureAwait(false);

            if (cts.Token.IsCancellationRequested)
            {
                return;
            }

            await action(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when debounce is reset
        }
        catch (Exception)
        {
            // Catch any unexpected exceptions to prevent unobserved task exceptions
        }
        finally
        {
            lock (_lock)
            {
                if (_debounceTokens.TryGetValue(key, out var token) && token == cts)
                {
                    _debounceTokens.Remove(key);
                }
            }
            cts.Dispose();
        }
    }

    public void Dispose()
    {
        List<CancellationTokenSource>? tokensToDispose;

        lock (_lock)
        {
            if (_debounceTokens.Count == 0)
            {
                return;
            }

            tokensToDispose = [.. _debounceTokens.Values];
            _debounceTokens.Clear();
        }

        foreach (var cts in tokensToDispose)
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch
            {
                // Ignore any exceptions during cleanup
            }
        }
    }
}
