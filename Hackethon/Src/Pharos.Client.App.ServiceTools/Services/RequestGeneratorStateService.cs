using System.Text.Json;
using Microsoft.JSInterop;

namespace Pharos.Client.App.ServiceTools.Services;

public interface IRequestGeneratorStateService
{
    Task<T?> GetStateAsync<T>(string componentId) where T : class;
    Task SetStateAsync<T>(string componentId, T state) where T : class;
}

internal sealed class RequestGeneratorStateService(IJSRuntime jsRuntime) : IRequestGeneratorStateService
{
    private const string _storagePrefix = "request_generator_state";
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetStateAsync<T>(string componentId) where T : class
    {
        var json = await jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", BuildStorageKey(componentId));

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public async Task SetStateAsync<T>(string componentId, T state) where T : class
    {
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        await jsRuntime.InvokeVoidAsync("sessionStorage.setItem", BuildStorageKey(componentId), json);
    }

    private static string BuildStorageKey(string componentId)
    {
        return $"{_storagePrefix}|{componentId}";
    }
}
