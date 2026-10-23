namespace Pharos.Client.Common.App.Auth;

/// <summary>
/// Short-hand for hiding content based on user permissions.
/// </summary>
public partial class PhAuthorize
{
    /// <summary>
    /// Show the content if the user has the specified permission.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Contracts.Security.Permissions"/> for standard permissions.
    /// </remarks>
    [Parameter] public string? Permission { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}