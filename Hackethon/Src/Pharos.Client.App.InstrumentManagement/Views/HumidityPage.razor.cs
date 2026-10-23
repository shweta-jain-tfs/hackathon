using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Breadcrumb;

namespace Pharos.Client.App.InstrumentManagement.Views;

public partial class HumidityPage : IDisposable
{
    [Inject] private ICurrentInstrumentViewModel InstrumentModel { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private HumidityViewModel ViewModel { get; set; } = null!;


    private List<BreadcrumbItem> Breadcrumbs =>
    [
        new(InstrumentModel.Name, PageRoute.Dashboard),
        new("Humidity", "")
    ];

    public bool Overview { get; set; } = true;

    // Define the overview sensor types.
    private static readonly List<EnumContinuousSensorType> _overviewSensorTypes =
    [
        EnumContinuousSensorType.EnvironmentRelativeHumidity,
        EnumContinuousSensorType.SampleDrawerAreaHumidity,
        EnumContinuousSensorType.ReagentDrawerAreaHumidity,
        EnumContinuousSensorType.FluidicsDrawerWashBreservoirHumidity,
        EnumContinuousSensorType.CoreIrwBowlSampleIncubationZoneRelativeHumidity,
        EnumContinuousSensorType.CoreIrwBowlConjugateIncubationZoneRelativeHumidity,
        EnumContinuousSensorType.CoreErwLidDevIncubationAirRelativeHumidity,
        EnumContinuousSensorType.BackStorageCartridgeAirLeftRelativeHumidity,
        EnumContinuousSensorType.BackStorageCartridgeAirRightRelativeHumidity,
        EnumContinuousSensorType.BackStorageCarrierAirUpperLeftRelativeHumidity,
        EnumContinuousSensorType.BackStorageCarrierAirLowerLeftRelativeHumidity,
        EnumContinuousSensorType.BackStorageCarrierAirUpperRightRelativeHumidity,
        EnumContinuousSensorType.BackStorageCarrierAirLowerRightRelativeHumidity,
    ];

    /// <summary>
    /// Gets the filtered list of sensors based on the overview setting.
    /// </summary>
    private IEnumerable<SensorValue> FilteredSensorList
    {
        get
        {
            var allSensors = ViewModel.Environment.HumidityList ?? Enumerable.Empty<SensorValue>();

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
        Overview = !ViewModel.Environment.HumidityList
            .Where(x => !_overviewSensorTypes.Contains(x.Sensor))
            .Any(x => x.OutsideLimit);
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