namespace Pharos.Client.App.Identity.Models
{
    public interface IApplicationUserDataService : IDetailsPageDataService<ApplicationUser, Guid>, IItemsSource<ApplicationUser>
    {
        Task ChangePasswordAsync(Guid userId, string newPassword);
        Task ChangeCurrentUserPasswordAsync(string currentPassword, string newPassword);
        Task<List<string>> GetUserNamesAsync();
        Task DeleteAsync(Guid userId);
        Task<ApplicationUser?> GetCurrentUserAsync();
    }
}
