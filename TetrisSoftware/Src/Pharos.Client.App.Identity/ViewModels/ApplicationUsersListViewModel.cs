using Pharos.Client.App.Identity.Models;

namespace Pharos.Client.App.Identity.ViewModels;

public sealed class ApplicationUsersListViewModel : ListViewModel<ApplicationUserViewModel, ApplicationUser>
{
    private readonly IApplicationUserDataService _dataService;

    public ApplicationUsersListViewModel(IApplicationUserDataService dataService) :base(dataService, null)
    {
        _dataService = dataService;
    }

    public async Task<Guid> CreateAsync(string userName, string password)
    {
        var newUser = new ApplicationUser { UserName = userName, Password = password };
        return await _dataService.CreateAsync(newUser);
    }

    public async Task DeleteUsers(Guid[] idList)
    {
        foreach (var id in idList)
        {
            await _dataService.DeleteAsync(id);
        }
    }

    protected override ApplicationUserViewModel CreateViewModel(ApplicationUser model)
    {
        return new ApplicationUserViewModel(model);
    }
}
