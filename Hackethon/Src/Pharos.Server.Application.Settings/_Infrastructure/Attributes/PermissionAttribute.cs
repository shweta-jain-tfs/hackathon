namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// Specifies the required permissions to access a settings sub-menu item or a settings field.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class PermissionAttribute : Attribute
{
    /// <summary>
    /// Specifies the name of the required permission
    /// </summary>
    public string Permission { get; }

    public PermissionAttribute(string permission)
    {
        Permission = permission;
    }
}
