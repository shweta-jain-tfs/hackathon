using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Breadcrumb;

namespace Pharos.Client.App.InstrumentManagement.Views;

public partial class TemperaturesPage : IDisposable
{
    [Inject] private FormatService FormatService { get; set; } = null!;
    [Inject] private ICurrentInstrumentViewModel InstrumentModel { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private TemperaturesViewModel ViewModel { get; set; } = null!;

    private string _temperatureRangeLocalized = "";
    private string _temperatureUnitLocalized = "";
    private TemperatureUnit _temperatureUnit;


    private List<BreadcrumbItem> Breadcrumbs =>
    [
        new(InstrumentModel.Name, PageRoute.Dashboard),
        new("Temperatures", "")
    ];

    public bool Overview { get; set; } = true;

    // Define the overview sensor types.
    private static readonly List<EnumContinuousSensorType> _overviewSensorTypes =
    [
        EnumContinuousSensorType.EnvironmentTemperature,
        EnumContinuousSensorType.SampleDrawerAreaTemperature,
        EnumContinuousSensorType.ReagentDrawerAreaTemperature,
        EnumContinuousSensorType.FluidicsDrawerWashAreservoirTemperature,
        EnumContinuousSensorType.CoreIrwBowlSampleIncubationZoneTemperature,
        EnumContinuousSensorType.CoreIrwBowlConjugateIncubationZoneTemperature,
        EnumContinuousSensorType.CoreErwLidDevIncubationAirTemperature,
        EnumContinuousSensorType.BackStorageCartridgeAirLeftTemperature,
        EnumContinuousSensorType.BackStorageCartridgeAirRightTemperature,
        EnumContinuousSensorType.FrontStorageCartridgeAirTemperature,
        EnumContinuousSensorType.BackStorageCarrierAirUpperLeftTemperature,
        EnumContinuousSensorType.BackStorageCarrierAirLowerLeftTemperature,
        EnumContinuousSensorType.BackStorageCarrierAirUpperRightTemperature,
        EnumContinuousSensorType.BackStorageCarrierAirLowerRightTemperature,
        EnumContinuousSensorType.FluorometerTemperature
    ];

    /// <summary>
    /// Gets the filtered list of sensors based on the overview setting.
    /// </summary>
    private IEnumerable<SensorValue> FilteredSensorList
    {
        get
        {
            var allSensors = ViewModel.Environment.TemperatureList ?? Enumerable.Empty<SensorValue>();

            // Get all sensors that are outside limit (always shown at top)
            var sensorValues = allSensors as SensorValue[] ?? allSensors.ToArray();
            var outsideLimitSensors = sensorValues
                .Where(x => x.OutsideLimit)
                .OrderBy(x => _overviewSensorTypes.Contains(x.Sensor)
                    ? _overviewSensorTypes.IndexOf(x.Sensor)
                    : int.MaxValue)
                .ThenBy(x => !_overviewSensorTypes.Contains(x.Sensor)
                    ? x.Sensor.ToLocalizedString("DashboardPage")
                    : string.Empty);

            // Get filtered sensors that are NOT outside limit
            var filteredSensors = (Overview
                    ? sensorValues.Where(x => _overviewSensorTypes.Contains(x.Sensor))
                    : sensorValues)
                .Where(x => !x.OutsideLimit) // Only non-outside-limit sensors
                .OrderBy(x => _overviewSensorTypes.Contains(x.Sensor)
                    ? _overviewSensorTypes.IndexOf(x.Sensor)
                    : int.MaxValue)
                .ThenBy(x => !_overviewSensorTypes.Contains(x.Sensor)
                    ? x.Sensor.ToLocalizedString("DashboardPage")
                    : string.Empty);

            // Combine: OutsideLimit sensors first, then filtered sensors
            return outsideLimitSensors.Concat(filteredSensors);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        await ViewModel.LoadAsync();

        // Set Overview as default if no additional (non-overview) sensors are out of limit.
        Overview = !ViewModel.Environment.TemperatureList
            .Where(x => !_overviewSensorTypes.Contains(x.Sensor))
            .Any(x => x.OutsideLimit);

        // Get temperature unit and set localized strings.
        _temperatureUnit = await FormatService.GetTemperatureUnitAsync();
        _temperatureRangeLocalized = _temperatureUnit.ToLocalizedString("TemperaturesPage_Range");
        _temperatureUnitLocalized = _temperatureUnit.ToLocalizedString("TemperaturesPage_Value");
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}