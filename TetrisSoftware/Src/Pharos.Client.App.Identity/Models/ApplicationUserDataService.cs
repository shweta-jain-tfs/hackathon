namespace Pharos.Client.App.Identity.Models;

internal class ApplicationUserDataService(HttpClient httpClient) : IApplicationUserDataService
{
    public async Task<Guid> CreateAsync(ApplicationUser newUser)
    {
        var client = new UsersClient(httpClient);
        var command = new CreateApplicationUserCommand
        {
            UserName = newUser.UserName,
            Password = newUser.Password
        };
        return await client.CreateApplicationUserAsync(command);
    }

    public async Task DeleteAsync(Guid userId)
    {
        var client = new UsersClient(httpClient);
        await client.DeleteApplicationUserAsync(userId);
    }

    public async Task<ApplicationUser> GetByIdAsync(Guid entityId)
    {
        var client = new UsersClient(httpClient);
        return await client.GetApplicationUserByIdAsync(entityId);
    }

    public async Task ChangePasswordAsync(Guid userId, string password)
    {
        var command = new ChangePasswordApplicationUserCommand
        {
            UserId = userId,
            Password = password
        };
        var client = new UsersClient(httpClient);
        await client.ChangePasswordApplicationUserAsync(command);
    }

    public async Task UpdateAsync(ApplicationUser value)
    {
        var client = new UsersClient(httpClient);
        var command = new UpdateApplicationUserCommand
        {
            UserName = value.UserName,
            FullName = value.FullName,
            UserId = value.Id,
            IsActive = value.IsActive,
            RoleId = value.Role?.Id,
            IsPinEnabled = value.IsPinEnabled,
        };
        await client.UpdateApplicationUserAsync(command);
    }

    public async Task<List<string>> GetUserNamesAsync()
    {
        var client = new UsersClient(httpClient);
        var users = await client.GetApplicationUsersAsync();
        return users.Select(u => u.UserName).ToList();
    }

    public async Task<List<ApplicationUser>> GetAsync()
    {
        var client = new UsersClient(httpClient);
        var result = await client.GetApplicationUsersAsync();
        return result.ToList();
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var client = new UsersClient(httpClient);
        return await client.GetCurrentUserDetailsAsync();
    }

    public async Task ChangeCurrentUserPasswordAsync(string currentPassword, string newPassword)
    {
        var command = new ChangeCurrentUserPasswordCommand
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword
        };

        var client = new UsersClient(httpClient);
        await client.ChangeCurrentUserPasswordAsync(command);
    }
}

