using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Pharos.Server.Application;

namespace Pharos.Server.Controllers;

[ApiController]
[Route("api/pharosinformation")]
public class PharosInformationController(ITetrisProtocolInformation protocolInformation, IPharosVersionService pharosVersion) : ControllerBase
{
    private const string _softwareDeviceIdentifierKey = "SoftwareDeviceIdentifier";
    private const string _softwareManufactureDateKey = "SoftwareManufactureDate";

    [HttpGet]
    public PharosInformationDto GetPharosInformation()
    {
        var programAssembly = typeof(Program).Assembly;

        var informationalVersion = programAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        var array = informationalVersion.Split('+');
        var gitVersionHash = array.Length == 2 ? array[1] : "Unknown git version";
        var softwareDeviceIdentifier = GetAssemblyMetadata(programAssembly, _softwareDeviceIdentifierKey);
        var softwareManufactureDateMetadata = GetAssemblyMetadata(programAssembly, _softwareManufactureDateKey);
        var softwareManufactureDate = string.IsNullOrEmpty(softwareManufactureDateMetadata)
            ? (DateTimeOffset?)null
            : DateTimeOffset.ParseExact(
                softwareManufactureDateMetadata,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal);

        return new PharosInformationDto
        {
            PharosVersion = pharosVersion.Version,
            SoftwareDeviceIdentifier = softwareDeviceIdentifier,
            SoftwareManufactureDate = softwareManufactureDate,
            GitVersionHash = gitVersionHash,
            ProtocolVersion = protocolInformation.VersionString
        };
    }

    private static string? GetAssemblyMetadata(Assembly assembly, string key)
    {
        return assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                   .SingleOrDefault(attribute => attribute.Key == key)
                   ?.Value;
    }
}

public class PharosInformationDto
{
    public string? PharosVersion { get; set; }
    public string? SoftwareDeviceIdentifier { get; set; }
    public DateTimeOffset? SoftwareManufactureDate { get; set; }
    public string? GitVersionHash { get; set; }
    public string? ProtocolVersion { get; set; }
}
