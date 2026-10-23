using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Pharos.Contracts.Security;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Domain;

public class ApplicationRole : IdentityRole<Guid>
{
    public const int DisplayNameLength = 50;

    /// <summary>
    /// Gets the role display name that is visible in the UI.
    /// </summary>
    [MaxLength(DisplayNameLength)]
    public string? DisplayName { get; set; } = default!;

    /// <summary>
    /// Gets the value indicating that this role is a built-in one and cannot be edited by a user.
    /// </summary>
    public bool IsBuiltInRole { get; set; }

    public static ApplicationRole CreatePredefinedOrBuiltInRole(string roleName)
    {
        if (roleName == BuiltInRoles.ServiceEngineer)
        {
            return new ApplicationRole
            {
                Name = BuiltInRoles.ServiceEngineer,
                DisplayName = Translations.ApplicationRole_ServiceEngineer,
                IsBuiltInRole = true
            };
        }

        if (roleName == BuiltInRoles.RndUser)
        {
            return new ApplicationRole
            {
                Name = BuiltInRoles.RndUser,
                DisplayName = Translations.ApplicationRole_RndUser,
                IsBuiltInRole = true
            };
        }
        if (PredefinedRoles.All.Contains(roleName))
        {
            var resourceName = "ApplicationRole_" + roleName;
            return new ApplicationRole
            {
                Name = roleName,
                DisplayName = Translations.ResourceManager.GetString(resourceName) ?? roleName,
                IsBuiltInRole = false
            };
        }
        throw new ArgumentException($"Unknown predefined or built-in user role '{roleName}'.");
    }

    public static IReadOnlyList<Claim> GetPredefinedOrBuiltInRolePermissions(string roleName)
    {
        // Predefined role permission sets
        HashSet<string> labAssistantPerms = new([
            Permissions.ManageReagents,
            Permissions.ManageSamples,
            Permissions.RunInstrumentMaintenance
        ]);

        HashSet<string> operatorPerms = new([
            Permissions.ManageInstruments,
            Permissions.ManageReagents,
            Permissions.ManageSamples,
            Permissions.ManageTestOrders,
            Permissions.RunInstrumentMaintenance,
            Permissions.ViewAuditTrail
        ]);

        HashSet<string> seniorOperatorPerms = new([
            Permissions.ApproveTestResults,
            Permissions.EditQualityControlSettings,
            Permissions.EditTestSettings,
            Permissions.ExportTestResults,
            Permissions.ManageInstruments,
            Permissions.ManageReagentStock,
            Permissions.ManageReagents,
            Permissions.ManageSamples,
            Permissions.ManageTestOrders,
            Permissions.RunInstrumentMaintenance,
            Permissions.ViewAuditTrail,
            Permissions.ViewPersonalAndHealthData
        ]);

        HashSet<string> administratorPerms = new([
            Permissions.ApproveTestResults,
            Permissions.EditQualityControlSettings,
            Permissions.EditReportTemplates,
            Permissions.EditSystemSettings,
            Permissions.EditTestSettings,
            Permissions.ExportTestResults,
            Permissions.ManageInstruments,
            Permissions.ManageReagentStock,
            Permissions.ManageReagents,
            Permissions.ManageSamples,
            Permissions.ManageTestOrders,
            Permissions.ManageUsersAndRoles,
            Permissions.RunInstrumentMaintenance,
            Permissions.ViewAuditTrail,
            Permissions.ViewPersonalAndHealthData
        ]);

        // Built-in role permission sets
        var serviceEngineerPerms = Permissions.All
            .Where(x =>
                x != Permissions.ApproveTestResults &&
                x != Permissions.ViewPersonalAndHealthData &&
                x != Permissions.ExportTestResults &&
                x != Permissions.ManageUsersAndRoles) // user management during system setup is handled dynamically
            .ToHashSet();

        var rndUserPerms = Permissions.All.ToHashSet();

        var permissions = roleName switch
        {
            BuiltInRoles.ServiceEngineer => serviceEngineerPerms,
            BuiltInRoles.RndUser => rndUserPerms,
            PredefinedRoles.LabAssistant => labAssistantPerms,
            PredefinedRoles.Operator => operatorPerms,
            PredefinedRoles.SeniorOperator => seniorOperatorPerms,
            PredefinedRoles.Administrator => administratorPerms,
            _ => throw new ArgumentException($"Unknown built-in user role '{roleName}'.")
        };

        var claims = new List<Claim>(permissions.Count);
        foreach (var perm in Permissions.All)
        {
            if (permissions.Contains(perm))
            {
                claims.Add(new Claim(Permissions.ClaimType, perm));
            }
        }
        return claims;
    }
}
