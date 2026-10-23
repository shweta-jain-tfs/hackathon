using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Application.Settings.LabCommunity;

[RootConfigSection(sortOrder: 95)]
[DisplayNameLocal("LabCommunitySettings")]
public class LabCommunitySettings
{
    [DisplayNameLocal("LabCommunitySettings_Enable")]
    public bool Enable { get; set; }

    [DependsFrom(nameof(Enable), "True", DependencyType.Disable)]
    [DisplayNameLocal("LabCommunitySettings_LaboratoryCode")]
    [MaxLength(Constants.LabCommunityLaboratoryCodeLength)]
    public string LaboratoryCode { get; set; } = string.Empty;

    [ProtectedData]
    [ExcludeFromImportExport]
    [DependsFrom(nameof(Enable), "True", DependencyType.Disable)]
    [DisplayNameLocal("LabCommunitySettings_UserName")]
    [MaxLength(Constants.LabCommunityUsernameLength)]
    public string Username { get; set; } = string.Empty;

    [ObfuscateSetting]
    [ProtectedData]
    [ExcludeFromImportExport]
    [DependsFrom(nameof(Enable), "True", DependencyType.Disable)]
    [DisplayNameLocal("LabCommunitySettings_Password")]
    [MaxLength(Constants.LabCommunityPasswordLength)]
    public string Password { get; set; } = string.Empty;

    // Utility property during development to easily test connection to lab community server. Not intended for production use and may be removed in the future.
    [DependsFrom(nameof(Enable), "True", DependencyType.Disable)]
    [DisplayNameLocal("LabCommunitySettings_ServerAddress")]
    public string ServerAddress { get; set; } = "https://labcommunity-devnew.phadia.com/api/";
}
