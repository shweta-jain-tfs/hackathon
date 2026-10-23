namespace Pharos.Client.App.Settings.ViewModels.Converters;

internal class Converter : IConverter
{
    private readonly ISettingsDataService<SettingItemBase> _dataService;
    private readonly Dictionary<Type, IComponentConverter> _converters;

    /// <summary>
    /// Initializes a new instance of the <see cref="Converter"/> class.
    /// </summary>
    /// <param name="dataService">The data service used to interact with the settings data.</param>
    /// <param name="converters">The collection of component converters.</param>
    public Converter(ISettingsDataService<SettingItemBase> dataService, IEnumerable<IComponentConverter> converters)
    {
        _dataService = dataService;
        _converters = converters.ToDictionary(c => c.Type);
    }

    public SettingsViewModelBase Convert(SettingItemBase item)
    {
        if (!_converters.TryGetValue(item.GetType(), out var converter))
        {
            throw new ArgumentException($"No converter for item of type [{item.GetType()}");
        }

        if (!converter!.TryConvert(item, this, _dataService, out var output))
        {
            throw new ArgumentException($"Could not convert item with id [{item.Key}");
        }

        return output!;
    }
}