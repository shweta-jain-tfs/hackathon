using Pharos.Contracts.Security;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Instrument;

[RootConfigSection(sortOrder: 80)]
[DisplayNameLocal("InstrumentSettings")]
[Permission(Permissions.EditSystemSettings)]
public class InstrumentSettings
{
    [DisplayNameLocal("InstrumentSettings_EnableAutomaticPowerSave")]
    public bool EnableAutomaticPowerSave { get; set; }

    [DisplayNameLocal("InstrumentSettings_PowerSaveTimeoutMinutes")]
    [DependsFrom(nameof(EnableAutomaticPowerSave), "True", DependencyType.Hide)]
    [SettingsRange(1, 60)] // Between 1 and 60 minutes
    public int PowerSaveTimeoutMinutes { get; set; } = 15;

    [ProviderPageList(InstrumentSettingsProviderKeys.InstrumentEntryProvider, nameof(InstrumentSpecificSettings.SerialNumber))]
    [ExcludeFromImportExport]
    [DisplayNameLocal("InstrumentSettings_Instruments_UNTRANSLATED")]
    public List<InstrumentSpecificSettings> Instruments { get; set; } = [];

    /// <summary>
    /// Finds the instrument-specific settings for the given serial number, or returns default values if not found.
    /// </summary>
    public InstrumentSpecificSettings GetInstrumentSettings(string serialNumber)
    {
        return Instruments.FirstOrDefault(i => string.Equals(i.SerialNumber, serialNumber, StringComparison.OrdinalIgnoreCase))
            ?? new InstrumentSpecificSettings { SerialNumber = serialNumber };
    }
}

public class InstrumentSpecificSettings
{
    /// <summary>
    /// Serial number of the instrument. This is used to identify the specific settings for each instrument.
    /// It should be unique for each instrument.
    /// This property is referenced from the ProviderPageList attribute (Instruments property),
    /// and is automatically hidden in the UI by the settings framework.
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    [SettingsGroup]
    [DisplayNameLocal("InstrumentSettings_Reagents")]
    public Reagents Reagents { get; set; } = new();

    [SettingsGroup]
    [DisplayNameLocal("InstrumentSettings_Waste")]
    public Waste Waste { get; set; } = new();

    [SettingsGroup]
    [DisplayNameLocal("InstrumentSettings_Rinse")]
    public Rinse Rinse { get; set; } = new();

    [SettingsGroup]
    [DisplayNameLocal("InstrumentSettings_DrawerLock")]
    public DrawerLock DrawerLock { get; set; } = new();
}

public class Waste
{
    [Permission(Permissions.EditSystemSettings)]
    [DisplayNameLocal("InstrumentSettings_MaximumWasteVolume")]
    [SettingsRange(5, 10, "liters")]
    public int MaximumLiquidWasteFillingVolume { get; set; } = 10;

    [Permission(Permissions.EditServiceSettings)]
    [DisplayNameLocal("InstrumentSettings_Waste_PipedOutInstalled")]
    [TableColumn]
    public bool PipedOutWasteInstalled { get; set; } = false;

    [Permission(Permissions.EditSystemSettings)]
    [DisplayNameLocal("InstrumentSettings_Waste_PipedOutActive")]
    [TableColumn]
    public bool PipedOutActive { get; set; } = false;
}

public class Rinse
{
    [Permission(Permissions.EditServiceSettings)]
    [DisplayNameLocal("InstrumentSettings_Rinse_PipedInInstalled")]
    [TableColumn]
    public bool PipedInRinseInstalled { get; set; } = false;

    [Permission(Permissions.EditSystemSettings)]
    [DisplayNameLocal("InstrumentSettings_Rinse_PipedInActive")]
    [TableColumn]
    public bool PipedInActive { get; set; } = false;
}

public class DrawerLock
{
    [Permission(Permissions.EditSystemSettings)]
    [DisplayNameLocal("InstrumentSettings_DrawerLock_AutoLockCompartmentTimeout")]
    [SettingsRange(5, 60, "seconds")]
    public int AutoLockCompartmentTimeout { get; set; } = 15;
}

public class Reagents
{
    [Permission(Permissions.EditServiceSettings)]
    [DisplayNameLocal("InstrumentSettings_Reagents_KeepExpiredReagentsOnBoard")]
    public bool KeepExpiredReagentsOnBoard { get; set; } = false;

    [Permission(Permissions.EditServiceSettings)]
    [DisplayNameLocal("InstrumentSettings_Reagents_KeepEmptyReagentsOnBoard")]
    public bool KeepEmptyReagentsOnBoard { get; set; } = false;
}
