using Pharos.Client.App.Identity.Models;

namespace Pharos.Client.App.Identity.ViewModels;

internal class RoleListViewModel : ListViewModel<RoleViewModel, Role>
{
    private readonly IRoleDataService _dataService;

    public RoleListViewModel(IRoleDataService dataService) : base(dataService, null)
    {
        _dataService = dataService;
    }

    public async Task<Guid> CreateAsync(string displayName)
    {
        return await _dataService.CreateAsync(new Role { DisplayName = displayName });
    }

    public async Task DeleteRolesAsync(List<Guid> idList)
    {
        foreach (var id in idList)
        {
            await _dataService.DeleteAsync(id);
        }
    }

    protected override RoleViewModel CreateViewModel(Role model)
    {
        return new RoleViewModel(model);
    }
}