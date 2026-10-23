namespace Pharos.Server.Application.Identity;

public class RoleDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public bool IsBuiltInRole { get; set; }
    public int? UsersInRole { get; set; }
    public List<string>? Permissions { get; set; }
    public bool IncludesCurrentUser { get; set; }
}
