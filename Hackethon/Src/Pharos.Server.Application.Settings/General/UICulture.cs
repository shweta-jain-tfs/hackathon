using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.General;

/// <summary>
/// Supported UI cultures. Enum value is the culture name with underscore instead of dash.
/// </summary>
public enum UICulture
{
    [DisplayNameLocal("Culture_en-US")]
    en_US,

    [DisplayNameLocal("Culture_ja-JP")]
    ja_JP,

    [DisplayNameLocal("Culture_sv-SE")]
    sv_SE,
}
