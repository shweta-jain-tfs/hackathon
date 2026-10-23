using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Services;

internal class RemoteCallSynchronizer(ILogger<RemoteCallSynchronizer> logger) : IRemoteCallSynchronizer
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

    public async Task<bool> WaitForExecutionContextAsync(string instrumentNumber, TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(instrumentNumber))
        {
            throw new ArgumentException("Instrument number must be provided", nameof(instrumentNumber));
        }

        var semaphore = _locks.GetOrAdd(instrumentNumber, _ => new SemaphoreSlim(1, 1));
        var acquired = await semaphore.WaitAsync(timeout).ConfigureAwait(false);
        if (acquired)
        {
            logger.LogDebug("Acquired gRPC execution context for {Instrument}.", instrumentNumber);
        }
        else
        {
            logger.LogError("Failed to acquire a gRPC execution context for {Instrument}.", instrumentNumber);
        }
        return acquired;
    }

    public bool WaitForExecutionContext(string instrumentNumber, TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(instrumentNumber))
        {
            throw new ArgumentException("Instrument number must be provided ", nameof(instrumentNumber));
        }

        var semaphore = _locks.GetOrAdd(instrumentNumber, _ => new SemaphoreSlim(1, 1));
        var acquired = semaphore.Wait(timeout);
        if (acquired)
        {
            logger.LogDebug("Acquired gRPC execution context for {Instrument}.", instrumentNumber);
        }
        else
        {
            logger.LogError("Failed to acquire a gRPC execution context for {Instrument}.", instrumentNumber);
        }
        return acquired;
    }

    public void ReleaseExecutionContext(string instrumentNumber)
    {
        if (string.IsNullOrWhiteSpace(instrumentNumber))
        {
            throw new ArgumentException("Instrument number must be provided", nameof(instrumentNumber));
        }

        if (_locks.TryGetValue(instrumentNumber, out var semaphore))
        {
            // Release only if currently held to avoid SemaphoreFullException
            // We attempt a non-blocking check by trying to wait with zero timeout and then immediately releasing twice.
            // However, this can interfere. Safer approach: try release and swallow SemaphoreFullException.
            try
            {
                semaphore.Release();
                logger.LogDebug("Released gRPC execution context for {Instrument}.", instrumentNumber);
            }
            catch (SemaphoreFullException)
            {
                logger.LogError("Failed to release gRPC execution context for {Instrument}.", instrumentNumber);
            }
        }
    }
}
