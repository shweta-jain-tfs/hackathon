using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Domain;

internal class InstrumentEnvironment
{
    private readonly Dictionary<EnumContinuousSensorType, EnumUnit> _sensorUnits = new()
    {
        //ERW Lid Air Development Incubation temperature (deg C)
        { EnumContinuousSensorType.CoreErwLidDevIncubationAirTemperature, EnumUnit.Celsius },

        //ERW Lid Air Development Incubation humidity (RH%)
        { EnumContinuousSensorType.CoreErwLidDevIncubationAirRelativeHumidity, EnumUnit.RelativeHumidity },

        //ERW Lid Air Development Incubation temperature (precision sensor) (deg C)
        { EnumContinuousSensorType.CoreErwLidDevIncubationAirTemperaturePrecision, EnumUnit.Celsius },

        //ERW Lid Air Elute Aspiration temperature (deg C)
        { EnumContinuousSensorType.CoreErwLidEluteAspirationAirTemperature, EnumUnit.Celsius },

        //ERW Lid Air Elute Aspiration humidity (RH%)
        { EnumContinuousSensorType.CoreErwLidEluteAspirationAirRelativeHumidity, EnumUnit.RelativeHumidity },

        //ERW Lid Air Elute Aspiration temperature (precision sensor) (deg C)
        { EnumContinuousSensorType.CoreErwLidEluteAspirationAirTemperaturePrecision, EnumUnit.Celsius },

        //IRW Air Sample Incubation temperature (precision sensor) (deg C)
        { EnumContinuousSensorType.CoreIrwLidSampleIncubationZoneAirTemperaturePrecision, EnumUnit.Celsius },

        //IRW Air Conjugate Incubation temperature (precision sensor) (deg C)
        { EnumContinuousSensorType.CoreIrwLidConjugateIncubationZoneAirTemperaturePrecision, EnumUnit.Celsius },

        //Aluminium IRW Lid temperature (precision sensor) (deg C)
        { EnumContinuousSensorType.CoreIrwLidRingTongueAluminumTemperaturePrecision, EnumUnit.Celsius },

        //Aluminium ERW Lid temperature (precision sensor) (deg C)
        { EnumContinuousSensorType.CoreErwLidRingTongueAluminumTemperaturePrecision, EnumUnit.Celsius },
        
        //Aluminium IRW Bowl temperature (precision sensor) (deg C)
        { EnumContinuousSensorType.CoreIrwBowlRingTongueAluminumTemperaturePrecision, EnumUnit.Celsius },

        //Aluminium IRW Bowl Fan temperature (precision sensor) (deg C)
        { EnumContinuousSensorType.CoreIrwBowlRingTongueFanAluminumTemperaturePrecision, EnumUnit.Celsius },

        //Aluminium ERW Bowl temperature (precision sensor) (deg C)
        { EnumContinuousSensorType.CoreErwBowlRingTongueAluminumTemperaturePrecision, EnumUnit.Celsius },

        //Aluminium Air Heater Box temperature (precision sensor) (deg C)
        { EnumContinuousSensorType.CoreAirHeaterBoxRingTongueAluminumTemperaturePrecision, EnumUnit.Celsius },

        //IRW Bowl Sample Incubation temperature (deg C)
        { EnumContinuousSensorType.CoreIrwBowlSampleIncubationZoneTemperature, EnumUnit.Celsius },

        //IRW Bowl Sample Incubation humidity (RH%)
        { EnumContinuousSensorType.CoreIrwBowlSampleIncubationZoneRelativeHumidity, EnumUnit.RelativeHumidity },

        //IRW Bowl Sample Incubation temperature (precision sensor) (deg C)
        { EnumContinuousSensorType.CoreIrwBowlSampleIncubationZoneTemperaturePrecision, EnumUnit.Celsius },

        //IRW Bowl Conjugate Incubation temperature (deg C)
        { EnumContinuousSensorType.CoreIrwBowlConjugateIncubationZoneTemperature, EnumUnit.Celsius },

        //IRW Bowl Conjugate Incubation humidity (RH%)
        { EnumContinuousSensorType.CoreIrwBowlConjugateIncubationZoneRelativeHumidity, EnumUnit.RelativeHumidity },

        //IRW Bowl Conjugate Incubation temperature (precision sensor) (deg C)
        { EnumContinuousSensorType.CoreIrwBowlConjugateIncubationZoneTemperaturePrecision, EnumUnit.Celsius },

        //Sample Drawer temperature (deg C)
        { EnumContinuousSensorType.SampleDrawerAreaTemperature, EnumUnit.Celsius },

        //Sample Drawer humidity (RH%)
        { EnumContinuousSensorType.SampleDrawerAreaHumidity, EnumUnit.RelativeHumidity },

        //Reagent Drawer temperature (deg C)
        { EnumContinuousSensorType.ReagentDrawerAreaTemperature, EnumUnit.Celsius },

        //Reagent Drawer humidity (RH%)
        { EnumContinuousSensorType.ReagentDrawerAreaHumidity, EnumUnit.RelativeHumidity },

        //Bottle Reagent Drawer temperature (deg C)
        { EnumContinuousSensorType.FluidicsDrawerWashAreservoirTemperature, EnumUnit.Celsius },

        //Bottle Reagent Drawer humidity (RH%)
        { EnumContinuousSensorType.FluidicsDrawerWashBreservoirHumidity, EnumUnit.RelativeHumidity },

        //Back Storage Desiccant Air A humidity (RH%)
        { EnumContinuousSensorType.BackStorageDesiccantAirArelativeHumidity, EnumUnit.RelativeHumidity },

        //Back Storage Desiccant Air B humidity (RH%)
        { EnumContinuousSensorType.BackStorageDesiccantAirBrelativeHumidity, EnumUnit.RelativeHumidity },

        //Back Storage Desiccant Air temperature (deg C)
        { EnumContinuousSensorType.BackStorageDesiccantAirTemperature, EnumUnit.Celsius },

        //Back Storage Desiccant Air temperature (precision sensor)
        { EnumContinuousSensorType.BackStorageDesiccantAirTemperaturePrecision, EnumUnit.Celsius },

        //Back Storage Cartridge Air temperature (precision sensor)
        { EnumContinuousSensorType.BackStorageCartridgeAirTemperaturePrecision, EnumUnit.Celsius },

        //Back Storage Cartridge Air Right temperature (deg C)
        { EnumContinuousSensorType.BackStorageCartridgeAirRightTemperature, EnumUnit.Celsius },

        //Back Storage Cartridge Air Right humidity (RH%)
        { EnumContinuousSensorType.BackStorageCartridgeAirRightRelativeHumidity, EnumUnit.RelativeHumidity },

        //Back Storage Cartridge Air Left temperature (deg C)
        { EnumContinuousSensorType.BackStorageCartridgeAirLeftTemperature, EnumUnit.Celsius },

        //Back Storage Cartridge Air Left humidity (RH%)
        { EnumContinuousSensorType.BackStorageCartridgeAirLeftRelativeHumidity, EnumUnit.RelativeHumidity },

        //Back Storage Carrier Air Upper Right temperature (deg C)
        { EnumContinuousSensorType.BackStorageCarrierAirUpperRightTemperature, EnumUnit.Celsius },

        //Back Storage Carrier Air Upper Right humidity (RH%)
        { EnumContinuousSensorType.BackStorageCarrierAirUpperRightRelativeHumidity, EnumUnit.RelativeHumidity },

        //Back Storage Carrier Air Lower Right temperature (deg C)
        { EnumContinuousSensorType.BackStorageCarrierAirLowerRightTemperature, EnumUnit.Celsius },

        //Back Storage Carrier Air Lower Right humidity (RH%)
        { EnumContinuousSensorType.BackStorageCarrierAirLowerRightRelativeHumidity, EnumUnit.RelativeHumidity },

        //Back Storage Carrier Air Upper Left temperature (deg C)
        { EnumContinuousSensorType.BackStorageCarrierAirUpperLeftTemperature, EnumUnit.Celsius },

        //Back Storage Carrier Air Upper Left humidity (RH%)
        { EnumContinuousSensorType.BackStorageCarrierAirUpperLeftRelativeHumidity, EnumUnit.RelativeHumidity },

        //Back Storage Carrier Air Lower Left temperature (deg C)
        { EnumContinuousSensorType.BackStorageCarrierAirLowerLeftTemperature, EnumUnit.Celsius },

        //Back Storage Carrier Air Lower Left humidity (RH%)
        { EnumContinuousSensorType.BackStorageCarrierAirLowerLeftRelativeHumidity, EnumUnit.RelativeHumidity },

        //Back Storage Carrier Elevator Air Upper temperature (deg C)
        { EnumContinuousSensorType.BackStorageCarrierAirUpperElevatorTemperature, EnumUnit.Celsius },

        //Back Storage Carrier Elevator Air Upper humidity (RH%)
        { EnumContinuousSensorType.BackStorageCarrierAirUpperElevatorRelativeHumidity, EnumUnit.RelativeHumidity },

        //Back Storage Carrier Elevator Air Lower temperature (deg C)
        { EnumContinuousSensorType.BackStorageCarrierAirLowerElevatorTemperature, EnumUnit.Celsius },

        //Back Storage Carrier Elevator Air Lower humidity (RH%)
        { EnumContinuousSensorType.BackStorageCarrierAirLowerElevatorRelativeHumidity, EnumUnit.RelativeHumidity },

        //Front Storage Air temperature (deg C)
        { EnumContinuousSensorType.FrontStorageCartridgeAirTemperature, EnumUnit.Celsius },

        //Front Storage Peltier1 cold temperature (mounted to bowl, above cold side of Peltier) (deg C)
        { EnumContinuousSensorType.FrontStorageCartridgePeltier1ColdTemperature, EnumUnit.Celsius },

        //Front Storage Peltier1 hot temperature (mounted in heat sink, hot side of Peltier) (deg C)
        { EnumContinuousSensorType.FrontStorageCartridgePeltier1HotTemperature, EnumUnit.Celsius },

        //Front Storage Peltier2 cold temperature (mounted to bowl, above cold side of Peltier) (deg C)
        { EnumContinuousSensorType.FrontStorageCartridgePeltier2Temperature, EnumUnit.Celsius },

        //Front Storage Peltier2 hot temperature (mounted in heat sink, hot side of Peltier) (deg C)
        { EnumContinuousSensorType.FrontStorageCartridgePeltier2HotTemperature, EnumUnit.Celsius },

        //Front Storage Peltier3 cold temperature (mounted to bowl, above cold side of Peltier) (deg C)
        { EnumContinuousSensorType.FrontStorageCartridgePeltier3Temperature, EnumUnit.Celsius },

        //Front Storage Peltier3 hot temperature (mounted in heat sink, hot side of Peltier) (deg C)
        { EnumContinuousSensorType.FrontStorageCartridgePeltier3HotTemperature, EnumUnit.Celsius },

        //Environment temperature (deg C)
        { EnumContinuousSensorType.EnvironmentTemperature, EnumUnit.Celsius },

        //Environment humidity (RH%)
        { EnumContinuousSensorType.EnvironmentRelativeHumidity, EnumUnit.RelativeHumidity },

        //Fluorometer temperature (deg C)
        { EnumContinuousSensorType.FluorometerTemperature, EnumUnit.Celsius }
    };

    private readonly List<ContinuousSensor> _sensors;

    public InstrumentEnvironment()
    {
        _sensors = new();
        InitializeSensors();
    }

    private void InitializeSensors()
    {
        foreach (var sensor in Enum.GetValues<EnumContinuousSensorType>().Where(x => x != EnumContinuousSensorType.UnspecifiedContinuousSensorType))
        {
            _sensors.Add(new ContinuousSensor
            {
                Type = sensor,
                Unit = _sensorUnits[sensor],
                OutsideLimit = false
            });
        }
    }

    public IReadOnlyList<ContinuousSensor> Sensors => _sensors;
}