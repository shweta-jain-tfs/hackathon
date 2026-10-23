namespace Pharos.Client.App.Logs.ViewModels.Errors;

public sealed class SystemErrorsInstrumentFilterViewModel
{
    private readonly InstrumentFilterViewModel _instrumentFilterViewModel;

    public SystemErrorsInstrumentFilterViewModel(InstrumentFilterViewModel instrumentFilterViewModel)
    {
        _instrumentFilterViewModel = instrumentFilterViewModel;
    }

    public List<string>? DefaultFilterValuesSerial { get; private set; }

    public Dictionary<string, string> FilterDataSourceSerial { get; private set; } = [];

    public async Task LoadAsync()
    {
        await _instrumentFilterViewModel.LoadAsync();

        var systemLabel = Translations.ResourceManager.GetString("InstrumentFilter_System")!;
        var serialDataSource = _instrumentFilterViewModel.FilterDataSourceSerial;

        FilterDataSourceSerial = new Dictionary<string, string> { { SystemErrorsFilterTokens.System, systemLabel } }
            .Concat(serialDataSource)
            .ToDictionary(x => x.Key, x => x.Value);

        DefaultFilterValuesSerial = _instrumentFilterViewModel.DefaultFilterValueSerial is string serial
            ? [serial, SystemErrorsFilterTokens.System]
            : null;
    }
}
