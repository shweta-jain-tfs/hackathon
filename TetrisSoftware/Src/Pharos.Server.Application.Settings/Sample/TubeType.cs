using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Sample;

public class TubeType
{
    public const int MaxTubeTypeNameLength = 30;

    /// <summary>
    /// Name of the TubeType
    /// </summary>
    [MaxLength(MaxTubeTypeNameLength)]
    [Required]
    [Unique(true)]
    [DisplayNameLocal("RequestSettings_TubeType_Name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// TubeDefinitionId of the TubeType
    /// </summary>
    [Required]
    [Unique]
    [SettingsRange(1, 5)]
    [DisplayNameLocal("RequestSettings_TubeType_TubeDefinitionId")]
    public int TubeDefinitionId { get; set; }

    /// <summary>
    /// Shape of the bottom of the tube
    /// </summary>
    [DisplayNameLocal("RequestSettings_TubeType_BottomShape")]
    public BottomShapeType BottomShape { get; set; } = BottomShapeType.VShaped;

    /// <summary>
    /// The bottom height for a tube. Set between 1-105 mm
    /// </summary>
    [Required]
    [SettingsRange(1.0, 105.0, "mm")]
    [DisplayNameLocal("RequestSettings_TubeType_InnerBottomLevel")]
    public double InnerBottomLevel { get; set; } = 1.0;

    /// <summary>
    /// The inner diameter for a tube. Set between 10-17 mm
    /// </summary>
    [Required]
    [SettingsRange(10.0, 17.0, "mm")]
    [DisplayNameLocal("RequestSettings_TubeType_InnerDiameter")]
    public double InnerDiameter { get; set; } = 10.0;

    /// <summary>
    /// Gel separation layer detection mode
    /// </summary>
    [DisplayNameLocal("RequestSettings_TubeType_GelSeparationLayerDetection")]
    public GelSeparationLayerDetection GelSeparationLayerDetection { get; set; } = GelSeparationLayerDetection.DetectionOff;

    /// <summary>
    /// Volume calculation for tube (Enables advanced settings)
    /// </summary>
    [DisplayNameLocal("RequestSettings_TubeType_VolumeCalculation")]
    public bool VolumeCalculation { get; set; }

    // ------------- ADVANCED ----------------

    /// <summary>
    /// (ADVANCED) Diameter of the bottom of the tube
    /// </summary>
    [DependsFrom(nameof(VolumeCalculation), "True", DependencyType.Hide)]
    [DisplayNameLocal("RequestSettings_TubeType_BottomDiameter")]
    public double BottomDiameter { get; set; }

    /// <summary>
    /// (ADVANCED) Diameter of the conical of the tube
    /// </summary>
    [DependsFrom(nameof(VolumeCalculation), "True", DependencyType.Hide)]
    [DisplayNameLocal("RequestSettings_TubeType_ConicalDiameter")]
    public double ConicalDiameter { get; set; }

    /// <summary>
    /// (ADVANCED) Length of the conical of the tube
    /// </summary>
    [DependsFrom(nameof(VolumeCalculation), "True", DependencyType.Hide)]
    [DisplayNameLocal("RequestSettings_TubeType_ConicalLength")]
    public double ConicalLength { get; set; }

    /// <summary>
    /// (ADVANCED) Length of the straight part of the tube
    /// </summary>
    [DependsFrom(nameof(VolumeCalculation), "True", DependencyType.Hide)]
    [DisplayNameLocal("RequestSettings_TubeType_StraightPartLength")]
    public double StraightPartLength { get; set; }

}
