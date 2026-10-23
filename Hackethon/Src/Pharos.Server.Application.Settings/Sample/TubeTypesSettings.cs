using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Sample;

public class TubeTypesSettings
{
    [DynamicDropdown(nameof(TubeTypeList), nameof(TubeType.TubeDefinitionId))]
    [Required]
    [DisplayNameLocal("TubeTypesSettings_TubeDefinitionNormal")]
    public string NormalTubeDefinitionId { get; set; } = "";

    [DynamicDropdown(nameof(TubeTypeList), nameof(TubeType.TubeDefinitionId))]
    [Required]
    [DisplayNameLocal("TubeTypesSettings_TubeDefinitionPediatric")]
    public string PediatricTubeDefinitionId { get; set; } = "";

    /// <summary>
    /// List of the tube types
    /// </summary>
    [DisplayNameLocal("TubeTypesSettings_TubeTypes")]
    [PageListMaxItems(5)]
    public List<TubeType> TubeTypeList { get; set; } = new();
}
