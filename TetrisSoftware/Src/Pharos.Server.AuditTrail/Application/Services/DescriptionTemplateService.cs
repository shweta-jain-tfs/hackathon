using System.Text.Json;
using Microsoft.Extensions.Localization;
using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.Localization;

namespace Pharos.Server.AuditTrail.Application.Services;
internal class DescriptionTemplateService(IStringLocalizer<Translations> loc)
{
    private static readonly string[] _preferredKeys =
    [
        "Username",
        "SampleId",
        "PatientId",
        "RequestId",
        "TestId",
        "RoleName"
    ];

    internal virtual string GetLocalizedDescription(EventId eventId, string eventDataJson)
    {
        var resource = loc[$"AuditTrail_{eventId}"].ToString();
        if (string.IsNullOrWhiteSpace(eventDataJson))
        {
            return resource;
        }

        try
        {
            using var doc = JsonDocument.Parse(eventDataJson);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                return resource;
            }

            var firstEventData = root[0];
            if (!firstEventData.TryGetProperty("Data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Object)
            {
                return resource;
            }

            foreach (var key in _preferredKeys)
            {
                if (dataElement.TryGetProperty(key, out var value))
                {
                    return resource.Replace($"{{{key}}}", value.ToString());
                }
            }

            return resource;
        }
        catch
        {
            // Fallback if JSON is invalid
            return resource;
        }
    }
}

