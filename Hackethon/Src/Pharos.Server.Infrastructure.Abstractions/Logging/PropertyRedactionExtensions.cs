using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Infrastructure.Logging;

public static class PropertyRedactionExtensions
{
    public const string RedactSensitiveDataKey = "RedactSensitiveData";
    public const string RedactedValue = "**REDACTED**";

    private static readonly HashSet<string> _sensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "AccessToken",
        "ApiKey",
        "Authorization",
        "BirthDate",
        "ClientSecret",
        "Cookie",
        "Diagnosis",
        "Email",
        "IdToken",
        "MedicalRecordNumber",
        "PatientName",
        "PatientNumber",
        "PhoneNumber",
        "Phone",
        "Pin",
        "PinHash",
        "RefreshToken",
        "Secret",
        "SetCookie",
        "Token",
        "User",
        "UserName",
        "ZipCode"
    };

    public static bool IsRedactionEnabled(this IConfiguration configuration)
    {
#if DEBUG
        return !bool.TryParse(configuration[RedactSensitiveDataKey], out var configuredValue) || configuredValue;
#else
        if (Testing.TestEnvironment.IsXUnitDetected())
        {
            // In test environments, redaction can be disabled as in DEBUG builds
            return bool.TryParse(configuration[RedactSensitiveDataKey], out var configuredValue) && configuredValue;
        }
        return true; // In production, redaction is always enabled
#endif
    }

    public static bool IsSensitive(this JsonPropertyInfo property)
    {
        return property.Name.IsSensitive()
               || HasSensitiveAttribute(property.AttributeProvider)
               || (property.AttributeProvider is PropertyInfo propertyInfo && HasSensitiveInterfaceProperty(propertyInfo));
    }

    public static bool IsSensitive(this string propertyName)
    {
        return propertyName.Contains("password", StringComparison.InvariantCultureIgnoreCase) || _sensitivePropertyNames.Contains(propertyName);
    }

    public static string Redact(this string value, bool redactSensitiveData)
    {
        return redactSensitiveData && !string.IsNullOrWhiteSpace(value) ? RedactedValue : value;
    }
    private static bool HasSensitiveInterfaceProperty(PropertyInfo property)
    {
        return property.DeclaringType?.GetInterfaces()
            .Select(interfaceType => interfaceType.GetProperty(property.Name, property.PropertyType))
            .Any(interfaceProperty => interfaceProperty is not null && HasSensitiveAttribute(interfaceProperty)) == true;
    }

    private static bool HasSensitiveAttribute(ICustomAttributeProvider? attributeProvider)
    {
        return attributeProvider?.IsDefined(typeof(ProtectedDataAttribute), inherit: true) == true
               || attributeProvider?.IsDefined(typeof(ProtectedPersonalDataAttribute), inherit: true) == true
               || attributeProvider?.IsDefined(typeof(PersonalDataAttribute), inherit: true) == true;
    }
}
