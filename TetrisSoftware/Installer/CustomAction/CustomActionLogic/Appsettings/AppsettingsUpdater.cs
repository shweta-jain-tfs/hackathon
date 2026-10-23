using CustomAction.CustomActionLogic.Interfaces.AppSettings;
using CustomAction.Wrappers.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CustomAction.CustomActionLogic.Appsettings;

public class AppSettingsUpdater : IAppSettingsUpdater
{
    private readonly ISessionWrapper _session;
    private readonly IFileSystem _fileSystem;
    private readonly IAppSettingsLocator _locator;
    private readonly IAppSettingsOverrideOptions _options;

    public AppSettingsUpdater(ISessionWrapper session, IFileSystem fileSystem, IAppSettingsLocator locator, IAppSettingsOverrideOptions options)
    {
        _session = session;
        _fileSystem = fileSystem;
        _locator = locator;
        _options = options;
    }

    public void UpdateDataProtectionSetting()
    {
        _session.Log($"Setting EnableDataProtection to: {_options.EnableDataProtection}");

        var json = _fileSystem.ReadAllText(_locator.AppSettingsPath);
        var jsonDe = JsonConvert.DeserializeObject<JObject>(json);

        if (jsonDe["DataProtection"] is JObject dataProtection)
        {
            dataProtection["EnableDataProtection"] = bool.Parse(_options.EnableDataProtection);
        }

        json = JsonConvert.SerializeObject(jsonDe, Formatting.Indented);
        _fileSystem.WriteAllText(_locator.AppSettingsPath, json);
    }

}