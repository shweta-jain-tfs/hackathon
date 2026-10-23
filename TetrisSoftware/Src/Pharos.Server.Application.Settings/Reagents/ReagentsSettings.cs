using System.ComponentModel;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Reagents;

[RootConfigSection(sortOrder: 70)]
[DisplayNameLocal("ReagentsSettings")]
public class ReagentsSettings
{
    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_ImmunoCap")]
    public ReagentTypeSettings ImmunoCap { get; set; } = new()
    { WasteTypeEmpty = WasteType.Recyclable, WasteTypeNonEmpty = WasteType.Recyclable };

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_EliaWell")]
    public ReagentTypeSettings EliaWell { get; set; } = new()
    { WasteTypeEmpty = WasteType.Recyclable, WasteTypeNonEmpty = WasteType.Recyclable };

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_EliaCalibratorWell")]
    public ReagentTypeSettings EliaCalibratorWell { get; set; } = new()
    { WasteTypeEmpty = WasteType.Recyclable, WasteTypeNonEmpty = WasteType.Recyclable };

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_DilutionWell")]
    public ReagentTypeSettings DilutionWell { get; set; } = new()
    { WasteTypeEmpty = WasteType.Recyclable, WasteTypeNonEmpty = WasteType.Recyclable };

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_Conjugate")]
    public ReagentTypeSettings Conjugate { get; set; } = new()
    { WasteTypeEmpty = WasteType.Biohazard, WasteTypeNonEmpty = WasteType.Biohazard };

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_QualityControl")]
    public ReagentTypeSettings QualityControl { get; set; } = new()
    { WasteTypeEmpty = WasteType.Biohazard, WasteTypeNonEmpty = WasteType.Biohazard };

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_Diluent")]
    public ReagentTypeSettings Diluent { get; set; } = new()
    { WasteTypeEmpty = WasteType.Biohazard, WasteTypeNonEmpty = WasteType.Biohazard };

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_DevelopmentSolution")]
    public ReagentTypeSettings DevelopmentSolution { get; set; } = new()
    { WasteTypeEmpty = WasteType.Biohazard, WasteTypeNonEmpty = WasteType.Biohazard };

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_CurveControl")]
    public ReagentTypeSettings CurveControl { get; set; } = new()
    { WasteTypeEmpty = WasteType.Biohazard, WasteTypeNonEmpty = WasteType.Biohazard };

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_Calibrator")]
    public ReagentTypeSettings Calibrator { get; set; } = new()
    { WasteTypeEmpty = WasteType.Biohazard, WasteTypeNonEmpty = WasteType.Biohazard };

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_QualityControlTube")]
    public ReagentTypeSettings QualityControlTube { get; set; } = new()
    { WasteTypeEmpty = WasteType.Biohazard, WasteTypeNonEmpty = WasteType.Biohazard };

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_StopSolution")]
    public ReagentTypeRndSettings StopSolution { get; set; } = new();

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_WashConcentrate")]
    public ReagentTypeRndSettings WashConcentrate { get; set; } = new();

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_WashAdditive")]
    public ReagentTypeRndSettings WashAdditive { get; set; } = new();

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_MaintenanceConcentrate")]
    public ReagentTypeRndSettings MaintenanceConcentrate { get; set; } = new();

    [SettingsGroup]
    [DisplayNameLocal("ReagentsSettings_FluoroC")]
    public ReagentTypeRndSettings FluoroC { get; set; } = new();
}

public class ReagentTypeSettings
{

    [DisplayNameLocal("ReagentTypeSettings_WasteTypeEmpty")]
    public WasteType WasteTypeEmpty { get; set; }


    [DisplayNameLocal("ReagentTypeSettings_WasteTypeNonEmpty")]
    public WasteType WasteTypeNonEmpty { get; set; }

    [DefaultValue(false)]
    [DisplayNameLocal("ReagentTypeSettings_UseExpiredReagents")]
    public bool UseExpiredReagents { get; set; } // this is a temporary setting [maybe :) ] until we provide editing of this flag per article
}

public class ReagentTypeRndSettings
{
    [DefaultValue(false)]
    [DisplayNameLocal("ReagentTypeRndSettings_UseExpiredReagents")]
    public bool UseExpiredReagents { get; set; } // this is a temporary setting [maybe :) ] until we provide editing of this flag per article
}

/// <summary>
/// Defines types of waste for articles that are automatically disposed by the instrument.
/// Applicable for s and s.
/// </summary>
public enum WasteType
{
    /// <summary>
    /// A biohazard-waste that requires special handling and disposal procedures.
    /// </summary>
    [DisplayNameLocal("WasteType_Biohazard")]
    Biohazard,

    /// <summary>
    /// A regular waste that can be disposed of in a standard manner.
    /// </summary>
    [DisplayNameLocal("WasteType_Recyclable")]
    Recyclable
}
