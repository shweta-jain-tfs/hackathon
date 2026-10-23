using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Pharos.Server.Application;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Identity.Infrastructure.Authentication;

/// <summary>
/// Service for managing PIN login lockout state per instrument/device.
/// Uses memory cache to track consecutive failed PIN attempts per instrument.
/// This is a scoped service that uses the current instrument context from ICurrentInstrumentService.
/// </summary>
internal class PinLockoutService : IPinLockoutService
{
    private const string _lockoutCacheKeyPrefix = "PIN_LOCKOUT:";
    private const string _globalLockoutCacheKey = "PIN_LOCKOUT:GLOBAL";
    private const string _trackedInstrumentKeysCacheKey = "PIN_LOCKOUT:TRACKED_KEYS";

    private readonly IMemoryCache _cache;
    private readonly IOptionsSnapshot<SecuritySettings> _securitySettings;
    private readonly IDateTime _dateTime;
    private readonly ICurrentInstrumentService _currentInstrumentService;
    private readonly Lock _trackedKeysLock = new();

    public PinLockoutService(
        IMemoryCache cache,
        IOptionsSnapshot<SecuritySettings> securitySettings,
        IDateTime dateTime,
        ICurrentInstrumentService currentInstrumentService)
    {
        _cache = cache;
        _securitySettings = securitySettings;
        _dateTime = dateTime;
        _currentInstrumentService = currentInstrumentService;
    }

    public bool IsPinModeEnabled()
    {
        return _securitySettings.Value.EnablePinLogin && _currentInstrumentService.InstrumentId != null;
    }

    public async Task<bool> IsPinLoginSuspended()
    {
        if (!_securitySettings.Value.EnablePinLoginLockout)
        {
            return false;
        }

        if (TryGetActiveState(_globalLockoutCacheKey, out _))
        {
            return true;
        }

        var key = GetCacheKey(_currentInstrumentService.InstrumentId);
        return TryGetActiveState(key, out _);
    }

    public async Task<bool> RecordFailedAttempt()
    {
        if (!_securitySettings.Value.EnablePinLoginLockout)
        {
            return false;
        }

        if (TryGetActiveState(_globalLockoutCacheKey, out _))
        {
            return false;
        }

        var key = GetCacheKey(_currentInstrumentService.InstrumentId);
        TrackInstrumentKey(key);

        PinLockoutState state;
        if (_cache.TryGetValue<PinLockoutState>(key, out var existingState) && existingState != null)
        {
            if (existingState.LockoutEndTime.HasValue)
            {
                if (_dateTime.Now >= existingState.LockoutEndTime.Value)
                {
                    state = new PinLockoutState { FailedAttempts = 0, LockoutEndTime = null };
                }
                else
                {
                    return false;
                }
            }
            else
            {
                state = existingState;
            }
        }
        else
        {
            state = new PinLockoutState { FailedAttempts = 0, LockoutEndTime = null };
        }

        state.FailedAttempts++;

        var isLockedOut = false;
        if (state.FailedAttempts >= _securitySettings.Value.PinLoginLockoutMaxFailedAttempts)
        {
            var lockoutDuration = TimeSpan.FromMinutes(_securitySettings.Value.PinLoginLockoutDurationMinutes);
            state.LockoutEndTime = _dateTime.Now.Add(lockoutDuration);
            isLockedOut = true;

            SetState(_globalLockoutCacheKey, new PinLockoutState
            {
                FailedAttempts = state.FailedAttempts,
                LockoutEndTime = state.LockoutEndTime
            });
        }

        SetState(key, state);

        return isLockedOut;
    }

    public async Task ResetAttempts()
    {
        var key = GetCacheKey(_currentInstrumentService.InstrumentId);
        _cache.Remove(key);

        lock (_trackedKeysLock)
        {
            if (_cache.TryGetValue<HashSet<string>>(_trackedInstrumentKeysCacheKey, out var trackedKeys) && trackedKeys != null)
            {
                trackedKeys.Remove(key);
                if (trackedKeys.Count == 0)
                {
                    _cache.Remove(_trackedInstrumentKeysCacheKey);
                }
                else
                {
                    _cache.Set(_trackedInstrumentKeysCacheKey, trackedKeys);
                }
            }
        }
    }

    private bool TryGetActiveState(string key, out PinLockoutState? state)
    {
        state = null;

        if (!_cache.TryGetValue<PinLockoutState>(key, out var cachedState) || cachedState == null || !cachedState.LockoutEndTime.HasValue)
        {
            return false;
        }

        if (_dateTime.Now < cachedState.LockoutEndTime.Value)
        {
            state = cachedState;
            return true;
        }

        if (key == _globalLockoutCacheKey)
        {
            ClearAllAttemptsAfterGlobalLockoutRelease();
        }
        else
        {
            _cache.Remove(key);
        }

        return false;
    }

    private void ClearAllAttemptsAfterGlobalLockoutRelease()
    {
        _cache.Remove(_globalLockoutCacheKey);

        lock (_trackedKeysLock)
        {
            if (_cache.TryGetValue<HashSet<string>>(_trackedInstrumentKeysCacheKey, out var trackedKeys) && trackedKeys != null)
            {
                foreach (var trackedKey in trackedKeys)
                {
                    _cache.Remove(trackedKey);
                }
            }

            _cache.Remove(_trackedInstrumentKeysCacheKey);
        }
    }

    private void TrackInstrumentKey(string key)
    {
        lock (_trackedKeysLock)
        {
            if (!_cache.TryGetValue<HashSet<string>>(_trackedInstrumentKeysCacheKey, out var trackedKeys) || trackedKeys == null)
            {
                trackedKeys = new HashSet<string>(StringComparer.Ordinal);
            }

            trackedKeys.Add(key);
            _cache.Set(_trackedInstrumentKeysCacheKey, trackedKeys);
        }
    }

    private void SetState(string key, PinLockoutState state)
    {
        var cacheOptions = new MemoryCacheEntryOptions();
        if (state.LockoutEndTime.HasValue)
        {
            cacheOptions.AbsoluteExpiration = state.LockoutEndTime.Value.AddMinutes(1);
        }
        else
        {
            cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
        }

        _cache.Set(key, state, cacheOptions);
    }

    private static string GetCacheKey(int? instrumentId)
    {
        return instrumentId.HasValue
            ? $"{_lockoutCacheKeyPrefix}{instrumentId}"
            : $"{_lockoutCacheKeyPrefix}default";
    }

    /// <summary>
    /// Internal class to store PIN lockout state in cache
    /// </summary>
    private class PinLockoutState
    {
        public int FailedAttempts { get; set; }
        public DateTimeOffset? LockoutEndTime { get; set; }
    }
}
