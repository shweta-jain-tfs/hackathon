using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Domain;

namespace Pharos.Server.Application.Settings.Result;

[RootConfigSection(sortOrder: 50)]
[DisplayNameLocal("ResultSettings")]
public class ResultSettings
{
    [SettingsGroup]
    [DisplayNameLocal("ResultSettings_TextSubstitutionSettingsGroup")]
    public TextSubstitution TextSubstitution { get; set; } = new();

    [SettingsGroup]
    [DisplayNameLocal("ResultSettings_Export")]
    public ResultExportSettings Export { get; set; } = new();
}

public class TextSubstitution
{
    /// <summary>
    /// The text to use when no result is available
    /// </summary>
    [MaxLength(Constants.ConcentrationAsTextLength)]
    [DisplayNameLocal("ResultSettings_TextSubstitution_MissingResultText")]
    public string MissingResultText { get; set; } = "---";

    /// <summary>
    /// The text to use for responses that are considered "Low RU" (below 5.0)
    /// </summary>
    [MaxLength(Constants.ResponseAsTextLength)]
    [DisplayNameLocal("ResultSettings_TextSubstitution_LowResponseText")]
    public string LowResponseText { get; set; } = "Low RU";
}

public class ResultExportSettings
{
    [DisplayNameLocal("ResultSettings_Export_ExportOnAnalyticalRunEnd")]
    public bool ExportOnAnalyticalRunEnd { get; set; }

    [Required]
    [AbsoluteDirectoryPath]
    [DisplayNameLocal("ResultSettings_Export_Path")]
    [DependsFrom(nameof(ExportOnAnalyticalRunEnd), "true", DependencyType.Disable)]
    public string AnalyticalRunExportPath { get; set; } = "";

    [DisplayNameLocal("ResultSettings_Export_ExportOnCustomRunEnd")]
    [ShowOnlyInRndMode]
    public bool ExportOnCustomRunEnd { get; set; }

    [Required]
    [AbsoluteDirectoryPath]
    [DisplayNameLocal("ResultSettings_Export_Path")]
    [DependsFrom(nameof(ExportOnCustomRunEnd), "true", DependencyType.Disable)]
    [ShowOnlyInRndMode]
    public string CustomRunExportPath { get; set; } = "";
}
