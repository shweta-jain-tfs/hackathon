using System.Reflection;

namespace Pharos.Contracts.Security;

/// <summary>
/// The list of permissions must be synchronized with the "Pharos User Access Permissions" document (DocID 809463).
/// </summary>
public static class Permissions
{
    public const string ClaimType = "permission";

    private const string _prefix = "prm_";

    public const string ApproveTestResults = _prefix + "approve_results";
    public const string EditQualityControlSettings = _prefix + "edit_qc_opt";
    public const string EditReportTemplates = _prefix + "edit_report_templ";
    public const string EditSystemSettings = _prefix + "edit_sys_opt";
    public const string EditTestSettings = _prefix + "edit_test_opt";
    public const string ExportTestResults = _prefix + "export_results";
    public const string ManageInstruments = _prefix + "mng_instruments";
    public const string ManageReagents = _prefix + "mng_reagents";
    public const string ManageReagentStock = _prefix + "mng_stock";
    public const string ManageSamples = _prefix + "mng_samples";
    public const string ManageTestOrders = _prefix + "mng_test_orders";
    public const string ManageUsersAndRoles = _prefix + "mng_users_roles";
    public const string RunInstrumentMaintenance = _prefix + "run_maintenance";
    public const string RunServiceTools = _prefix + "run_svc_tools";
    public const string ViewAuditTrail = _prefix + "view_audit";
    public const string ViewPersonalAndHealthData = _prefix + "view_pii_phi";
    public const string EditServiceSettings = _prefix + "edit_svc_opt";
    public const string AccessInstrumentServiceUi = _prefix + "svc_ui";

    public static readonly IReadOnlyList<string> All =
        typeof(Permissions).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(x => x.FieldType == typeof(string) && x.GetValue(null)!.ToString()!.StartsWith(_prefix))
        .Select(x => (string)x.GetValue(null)!)
        .ToArray();
}
