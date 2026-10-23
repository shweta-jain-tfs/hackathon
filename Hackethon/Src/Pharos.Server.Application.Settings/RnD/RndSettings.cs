using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.RnD;

[RootConfigSection(sortOrder: 1000)]
[DisplayNameLocal("RndSettings")]
public class RndSettings
{
    [DisplayNameLocal("RndSettings_RndModel")]
    public bool RndMode { get; set; } = true; // Default to true, can be overridden in configuration

    [DisplayNameLocal("RndSettings_RuoMode")]
    public bool RuoMode { get; set; } = true; // Default to true, can be overridden in configuration
}
