
namespace Pharos.Contracts;

// Add properties to this class and update the server and client AuthenticationStateProviders
// to expose more information about the authenticated user to the client.
public class UserInfo
{
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public string[]? Permissions { get; set; }
    public string[]? Roles { get; set; }
    public bool RndMode { get; set; }
}