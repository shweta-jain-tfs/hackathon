using System;
using CustomAction.CustomActionLogic.Interfaces.AppSettings;
using CustomAction.Wrappers.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CustomAction.CustomActionLogic.Appsettings;
internal class AppSettingsReader : IAppSettingsReader
{
    private readonly ISessionWrapper _session;
    private readonly IFileSystem _fileSystem;
    private readonly IAppSettingsLocator _appSettingsLocator;

    private const string _pharosConnectionStringKey = "Default";

    public AppSettingsReader(ISessionWrapper session, IFileSystem fileSystem, IAppSettingsLocator appSettingsLocator)
    {
        _session = session;
        _fileSystem = fileSystem;
        _appSettingsLocator = appSettingsLocator;
    }

    public string GetPharosDbConnectionString()
    {
        var appSettingsPath = _appSettingsLocator.AppSettingsPath;

        _session.Log("Reading Pharos database connection string from appsettings at: " + appSettingsPath);

        if (string.IsNullOrWhiteSpace(appSettingsPath))
        {
            throw new ArgumentException("Appsettings path must be provided.", nameof(appSettingsPath));
        }

        try
        {
            if (!_fileSystem.Exists(appSettingsPath))
            {
                throw new InvalidOperationException($"Appsettings file not found at '{appSettingsPath}'.");
            }

            var json = _fileSystem.ReadAllText(appSettingsPath);
            var jsonDe = JsonConvert.DeserializeObject<JObject>(json);

            var connectionString = jsonDe?["ConnectionStrings"]?[_pharosConnectionStringKey]?.ToString();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("ConnectionStrings.Default is missing or empty in appsettings.json.");
            }

            return connectionString;
        }
        catch (Exception ex)
        {
            _session.Log($"Error reading connection string from '{appSettingsPath}': {ex.Message}");
            throw;
        }
    }
}
