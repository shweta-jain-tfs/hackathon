using System.Text.Json;

namespace Pharos.Client.Common.App.Services;

public class ProblemDetails
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Detail { get; set; }
    public string? Instance { get; set; }
    public string? TraceId { get; set; }
    public IDictionary<string, ICollection<string>> Errors { get; set; } = new Dictionary<string, ICollection<string>>();
}

public static class ProblemDetailsExtensions
{
    public static ProblemDetails? GetProblemDetails(this ApiException exception)
    {
        try
        {
            return JsonSerializer.Deserialize<ProblemDetails>(exception.Response, ExceptionHandlerService._jsonSerializerOptions);
        }
        catch (JsonException)
        {
            // suppress any JSON deserialization exceptions, as we want to fall back to a generic error message in that case, not crash the app.
            return null;
        }
    }
}