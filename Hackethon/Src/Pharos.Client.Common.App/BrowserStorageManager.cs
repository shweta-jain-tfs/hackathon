using Blazored.LocalStorage;

namespace Pharos.Client.Common.App;

public class BrowserStorageManager
{
    private readonly ILocalStorageService _localStorage;

    // Define predefined keys.
    public static class StorageKey
    {
        public const string CurrentCulture = "CurrentCulture";
    }

    public BrowserStorageManager(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>
    /// Get stored value.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task<string?> GetValue(string key)
    {
        var value = await _localStorage.GetItemAsync<string>(key);
        return await Task.FromResult(value);
    }

    /// <summary>
    /// Save value for a key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task SetValue(string key, string value)
    {
        await _localStorage.SetItemAsync(key, value);
    }
}