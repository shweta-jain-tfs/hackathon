using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using Pharos.Contracts.Security;

namespace Pharos.Server.Swagger;

/// <summary>
/// NSwag operation processor that adds permission requirements to Swagger documentation.
/// </summary>
internal class PermissionOperationProcessor : IOperationProcessor
{
    private static readonly HashSet<string> _knownPolicies =
    [
        Policies.InstrumentOnly,
        Policies.InstrumentOnlyAllowAnonymous,
        Policies.RnD,
        Policies.Ruo
    ];

    private static readonly Lazy<HashSet<string>> _allPermissions = new(LoadPermissions);

    private static readonly Dictionary<string, string> _permissionDescriptions = new()
    {
        [Permissions.ApproveTestResults] = "Approve test results",
        [Permissions.EditQualityControlSettings] = "Edit quality control settings",
        [Permissions.EditReportTemplates] = "Edit report templates",
        [Permissions.EditSystemSettings] = "Edit system settings",
        [Permissions.EditTestSettings] = "Edit test settings",
        [Permissions.ExportTestResults] = "Export test results",
        [Permissions.ManageInstruments] = "Manage instruments",
        [Permissions.ManageReagents] = "Manage reagents",
        [Permissions.ManageReagentStock] = "Manage reagent stock",
        [Permissions.ManageSamples] = "Manage samples",
        [Permissions.ManageTestOrders] = "Manage test orders",
        [Permissions.ManageUsersAndRoles] = "Manage users and roles",
        [Permissions.RunInstrumentMaintenance] = "Run instrument maintenance",
        [Permissions.RunServiceTools] = "Run service tools",
        [Permissions.ViewAuditTrail] = "View audit trail",
        [Permissions.ViewPersonalAndHealthData] = "View personal and health data",
        [Permissions.EditServiceSettings] = "Edit service settings",
        [Permissions.AccessInstrumentServiceUi] = "Access instrument service UI"
    };

    private readonly ILogger<PermissionOperationProcessor> _logger;

    public PermissionOperationProcessor(ILogger<PermissionOperationProcessor> logger)
    {
        _logger = logger;
    }

    public bool Process(OperationProcessorContext context)
    {
        if (context?.MethodInfo == null || context?.ControllerType == null || context?.OperationDescription?.Operation == null)
        {
            _logger.LogDebug("Skipping permission processing - context is incomplete");
            return true;
        }

        var methodName = $"{context.ControllerType.Name}.{context.MethodInfo.Name}";
        _logger.LogDebug("Processing permissions for {MethodName}", methodName);

        var authorizeAttributes = context.MethodInfo
            .GetCustomAttributes<AuthorizeAttribute>()
            .Concat(context.ControllerType.GetCustomAttributes<AuthorizeAttribute>())
            .ToList();

        if (authorizeAttributes.Count == 0)
        {
            _logger.LogDebug("No authorization attributes found for {MethodName}", methodName);
            return true;
        }

        var (permissions, policies) = CategorizeAttributes(authorizeAttributes);

        _logger.LogDebug("Found {PermissionCount} permissions and {PolicyCount} policies for {MethodName}",
            permissions.Count, policies.Count, methodName);

        if (permissions.Count > 0 || policies.Count > 0)
        {
            AppendAuthorizationInfo(context, permissions, policies);
            _logger.LogDebug("Added authorization info to Swagger for {MethodName}", methodName);
        }

        return true;
    }

    private static (List<string> permissions, List<string> policies) CategorizeAttributes(
        List<AuthorizeAttribute> attributes)
    {
        var permissions = new List<string>();
        var policies = new List<string>();

        foreach (var attr in attributes)
        {
            if (string.IsNullOrWhiteSpace(attr.Policy))
            {
                continue;
            }

            if (_knownPolicies.Contains(attr.Policy))
            {
                policies.Add(attr.Policy);
            }
            else if (_allPermissions.Value.Contains(attr.Policy))
            {
                permissions.Add(attr.Policy);
            }
            else
            {
                policies.Add(attr.Policy);
            }
        }

        return (permissions, policies);
    }

    private void AppendAuthorizationInfo(
        OperationProcessorContext context,
        List<string> permissions,
        List<string> policies)
    {
        var description = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(context.OperationDescription.Operation.Description))
        {
            description.Append(context.OperationDescription.Operation.Description);
        }

        description.Append("\n\n**Authorization Requirements:**");

        if (permissions.Count > 0)
        {
            description.Append("\n- **Required Permission(s):**");
            foreach (var permission in permissions)
            {
                var permissionDescription = _permissionDescriptions.TryGetValue(permission, out var desc)
                    ? desc
                    : permission;

                _logger.LogDebug("Permission {Permission}: Description='{Description}'",
                    permission, permissionDescription);

                description.Append($"\n  - `{permission}` - {permissionDescription}");
            }
        }

        if (policies.Count > 0)
        {
            description.Append($"\n- **Required Policy:** {string.Join(", ", policies.Select(p => $"`{p}`"))}");
        }

        description.Append("\n\n*Users without the required permissions will receive a 403 Forbidden response.*");

        context.OperationDescription.Operation.Description = description.ToString();
    }

    private static HashSet<string> LoadPermissions()
    {
        return typeof(Permissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => f.GetRawConstantValue() as string)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToHashSet()!;
    }
}
