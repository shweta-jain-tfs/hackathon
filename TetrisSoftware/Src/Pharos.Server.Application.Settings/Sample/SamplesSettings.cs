using Pharos.Contracts.Security;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Sample;

[RootConfigSection(sortOrder: 20)]
[DisplayNameLocal("SamplesSettings")]
[Permission(Permissions.EditSystemSettings)]
public class SamplesSettings : IConfigSectionWithInitialValues
{
    /// <summary>
    /// The settings for the tube type
    /// </summary>
    [DisplayNameLocal("RequestSettings_TubeTypesSettings")]
    public TubeTypesSettings TubeTypes { get; set; } = new();

    [SettingsGroup]
    [DisplayNameLocal("SamplesSettings_Tray")]
    public Tray Tray { get; set; } = new();

    [SettingsGroup]
    [DisplayNameLocal("SamplesSettings_Drawer")]
    public Drawer Drawer { get; set; } = new();

    [DisplayNameLocal("SpecialSampleHandlingSettings_Title")]
    public SpecialSampleHandlingSettings SpecialSampleHandling { get; set; } = new();

    [DisplayNameLocal("TubeBarcodeSettings_Title")]
    public TubeBarcodesSettings TubeBarcodes { get; set; } = new();

    /// <summary>
    /// When enabled, Sample IDs are matched case-insensitively when identifying requests (e.g. for test order matching).
    /// </summary>
    [DisplayNameLocal("SamplesSettings_CaseInsensitiveSampleId")]
    [HelpText(typeof(string), "SamplesSettings_CaseInsensitiveSampleId_HelpText")]
    public bool CaseInsensitiveSampleId { get; set; } = false;

    public void InitializeConfigSection(bool isDevelopment)
    {
        // Seed the default values for the RequestSettings
        TubeTypes = new TubeTypesSettings
        {
            TubeTypeList = new List<TubeType>
            {
                new()
                {
                    TubeDefinitionId = 1,
                    Name = "Normal",
                    InnerBottomLevel = 2.1,
                    InnerDiameter = 10,
                    BottomShape = BottomShapeType.UShaped,
                    GelSeparationLayerDetection = GelSeparationLayerDetection.DetectionOff
                },
                new()
                {
                    TubeDefinitionId = 2,
                    Name = "Pediatric",
                    InnerBottomLevel = 34.1,
                    InnerDiameter = 10,
                    BottomShape = BottomShapeType.UShaped,
                    GelSeparationLayerDetection = GelSeparationLayerDetection.DetectionOff
                }
            },

            // Default values for the dropdowns
            NormalTubeDefinitionId = "1",
            PediatricTubeDefinitionId = "2"
        };
    }
}

public class Drawer
{
    [DisplayNameLocal("SamplesSettings_Drawer_DrawerMode")]
    [DisplayModeRadioButtons]
    public DrawerMode DrawerMode { get; set; } = DrawerMode.BothDrawersAsInputAndOutput;
}

public class Tray
{
    [DisplayNameLocal("SamplesSettings_Tray_EnableEditingOfSampleId")]
    [HelpText(typeof(string), "SamplesSettings_Tray_EnableEditingOfSampleId_HelpText")]
    public bool EnableEditingOfSampleId { get; set; } = false;
}

public enum DrawerMode
{
    [DisplayNameLocal("SamplesSettings_DrawerMode_BothDrawersAsInputAndOutput")]
    BothDrawersAsInputAndOutput,

    [DisplayNameLocal("SamplesSettings_DrawerMode_LeftDrawerAsInputAndRightDrawerAsOutput")]
    LeftDrawerAsInputAndRightDrawerAsOutput
}
