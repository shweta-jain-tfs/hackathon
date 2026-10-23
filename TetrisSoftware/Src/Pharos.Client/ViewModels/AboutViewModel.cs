using Pharos.Client.App.SystemUpdate.Models;
using Pharos.Client.App.SystemUpdate.Notifications;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.ViewModels;

public sealed class AboutViewModel : LoadingViewModel, IDisposable
{
    private const string _fallbackPharosVersion = "0.0.0";
    private const string _fallbackSoftwareUdiVersion = "000000";
    private const string _fallbackSoftwareDeviceIdentifier = "00000000000000";

    private readonly ISingletonDataSource<PharosInformation> _informationDataSource;
    private readonly ISingletonDataSource<ModuleInformation> _moduleInformationDataSource;
    private readonly ICurrentInstrumentNotificationProvider _notificationProvider;
    private readonly ISystemUpdateNotificationProvider _systemUpdateNotificationProvider;
    private readonly ISystemUpdateDataService _systemUpdateDataService;

    public AboutViewModel(
        ISingletonDataSource<PharosInformation> informationDataSource,
        ISingletonDataSource<ModuleInformation> moduleInformationDataSource,
        ICurrentInstrumentNotificationProvider notificationProvider,
        ISystemUpdateNotificationProvider systemUpdateNotificationProvider,
        ISystemUpdateDataService systemUpdateDataService)
    {
        _informationDataSource = informationDataSource;
        _moduleInformationDataSource = moduleInformationDataSource;
        _notificationProvider = notificationProvider;
        _systemUpdateNotificationProvider = systemUpdateNotificationProvider;
        _systemUpdateDataService = systemUpdateDataService;

        _notificationProvider.ModuleInformationReceived += OnModuleInformationReceived;
        _systemUpdateNotificationProvider.UpdatesAvailable += OnUpdatesAvailable;
        _systemUpdateNotificationProvider.UpdateFailed += OnUpdateFailure;
    }

    public string PharosVersion { get; private set; } = _fallbackPharosVersion;
    public string SoftwareUdiVersion { get; private set; } = _fallbackSoftwareUdiVersion;
    public string SoftwareDeviceIdentifier { get; private set; } = _fallbackSoftwareDeviceIdentifier;
    public DateTimeOffset? SoftwareManufactureDate { get; private set; }
    public string? GitVersionHash { get; private set; }
    public string? ProtocolVersion { get; private set; }

    // Module Information properties
    public string? SerialNumber { get; private set; }
    public DateTimeOffset? InstallationDate { get; private set; }
    public string? InstrumentType { get; private set; }
    public List<Module>? Modules { get; private set; }
    public string? EulaUrl { get; set; }

    // System Update properties
    public List<string>? AvailableUpdates { get; private set; }
    public bool HasAvailableUpdates => AvailableUpdates is { Count: > 0 };
    public string? ChangeLogUrl { get; set; }
    public bool IsUpdating { get; set; }

    public async Task ApplyUpdatesAsync()
    {
        IsUpdating = true;
        OnPropertyChanged(nameof(IsUpdating));

        await _systemUpdateDataService.InitiateUpdate();
    }

    protected override async Task DoLoadAsync()
    {
        var information = await _informationDataSource.GetAsync();
        PharosVersion = GetFallbackIfMissing(information.PharosVersion, _fallbackPharosVersion);
        SoftwareUdiVersion = FormatUdiVersion(PharosVersion) ?? _fallbackSoftwareUdiVersion;
        SoftwareDeviceIdentifier = GetFallbackIfMissing(information.SoftwareDeviceIdentifier, _fallbackSoftwareDeviceIdentifier);
        SoftwareManufactureDate = information.SoftwareManufactureDate;
        GitVersionHash = information.GitVersionHash;
        ProtocolVersion = information.ProtocolVersion;

        AvailableUpdates = await _systemUpdateDataService.GetAvailableUpdates();

        await LoadModuleInformation();
    }

    private static string GetFallbackIfMissing(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string? FormatUdiVersion(string? version)
    {
        if (!Version.TryParse(version, out var parsedVersion))
        {
            return null;
        }

        var result = parsedVersion.Major.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)
                     + parsedVersion.Minor.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);

        if (parsedVersion.Build >= 0)
        {
            result += parsedVersion.Build.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (parsedVersion.Revision >= 0)
        {
            result += parsedVersion.Revision.ToString("D5", System.Globalization.CultureInfo.InvariantCulture);
        }

        return result;
    }

    private async void OnModuleInformationReceived(object? sender, EventArgs e)
    {
        await LoadModuleInformation();
    }

    private void OnUpdatesAvailable(object? sender, List<string> updates)
    {
        AvailableUpdates = updates;

        OnPropertyChanged(nameof(AvailableUpdates));
        OnPropertyChanged(nameof(HasAvailableUpdates));
    }

    private void OnUpdateFailure(object? sender, EventArgs e)
    {
        IsUpdating = false;
        OnPropertyChanged(nameof(IsUpdating));
    }

    private async Task LoadModuleInformation()
    {
        var moduleInformation = await _moduleInformationDataSource.GetAsync();

        SerialNumber = moduleInformation.SerialNumber;
        InstallationDate = moduleInformation.InstallationDate;
        InstrumentType = moduleInformation.Type.ToString();
        Modules = moduleInformation.Modules.Select(m => new Module
        {
            ModuleType = m.ModuleType.ToString(),
            ArticleNumber = m.ArticleNumber,
            Model = m.Model,
            Version = m.Version,
            SerialNumber = m.SerialNumber,
            InstallationDate = m.InstallationDate
        }).ToList();

        OnPropertyChanged(null);
    }

    public void Dispose()
    {
        _notificationProvider.ModuleInformationReceived -= OnModuleInformationReceived;
        _systemUpdateNotificationProvider.UpdatesAvailable -= OnUpdatesAvailable;
        _systemUpdateNotificationProvider.UpdateFailed -= OnUpdateFailure;
    }
}

public class Module
{
    public string? ModuleType { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Model { get; set; }
    public string? Version { get; set; }
    public string? SerialNumber { get; set; }
    public DateTimeOffset? InstallationDate { get; set; }
}
