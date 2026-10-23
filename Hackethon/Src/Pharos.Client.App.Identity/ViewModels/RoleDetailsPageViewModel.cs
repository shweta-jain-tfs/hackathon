using Pharos.Client.App.Identity.Models;

namespace Pharos.Client.App.Identity.ViewModels;

public class RoleDetailsPageViewModel : DetailsPageViewModel<RoleDetailsViewModel, Role, Guid>
{
    private readonly IRoleDataService _dataService;

    public RoleDetailsPageViewModel(IRoleDataService dataService) : base(dataService, null)
    {
        _dataService = dataService;
    }

    protected override Task<Role> CreateDefaultModel()
    {
        return Task.FromResult(new Role());
    }

    protected override async Task<RoleDetailsViewModel> CreateViewModel(Role model)
    {
        var result = await _dataService.GetAsync();

        var otherRoleNames = result
            .Where(x => x.DisplayName != model.DisplayName)
            .Select(x => x.DisplayName)
            .ToList();

        return new RoleDetailsViewModel(model, otherRoleNames);
    }
}