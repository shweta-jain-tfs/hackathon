namespace Pharos.Client.App.Identity.Models;

public interface IRoleDataService : IItemsSource<Role>, IDetailsPageDataService<Role, Guid>
{
    Task DeleteAsync(Guid id);
}

internal class RoleDataService(HttpClient httpClient) : IRoleDataService
{
    public async Task<List<Role>> GetAsync()
    {
        var result = await new RolesClient(httpClient).GetRolesAsync();
        return result.ToList();
    }

    public async Task<Role> GetByIdAsync(Guid entityId)
    {
        return await new RolesClient(httpClient).GetRoleByIdAsync(entityId);
    }

    public async Task<Guid> CreateAsync(Role role)
    {
        return await new RolesClient(httpClient).CreateRoleAsync(new CreateRoleCommand { DisplayName = role.DisplayName });
    }

    public async Task DeleteAsync(Guid entityId)
    {
        await new RolesClient(httpClient).DeleteRoleAsync(entityId);
    }

    public async Task<DeleteResponse> DeleteAsync(Guid[] entityIds)
    {
        //:ag check pre-cond & make atomic
        var client = new RolesClient(httpClient);
        foreach (var entityId in entityIds)
        {
            await client.DeleteRoleAsync(entityId);
        }

        return new DeleteResponse();
    }

    public async Task UpdateAsync(Role value)
    {
        var command = new UpdateRoleCommand
        {
            DisplayName = value.DisplayName,
            RoleId = value.Id,
            Permissions = value.Permissions
        };
        await new RolesClient(httpClient).UpdateRoleAsync(command);
    }
}
