using System.Net.Http.Json;
using MediatR.NotificationPublishers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class RequestorSearchIntegrationTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    [Fact]
    [Trait("ModuleRequirement", "MR_4002")]
    public async Task GlobalSearch_RequestorVisibility_FollowsSettingsAndPermissions()
    {
        await WaitForBackgroundNotificationsAsync();

        var userWithPermission = Guid.NewGuid();
        var userWithoutPermission = Guid.NewGuid();

        var requestorName = $"ReqName_{Guid.NewGuid():N}";
        var requestorId = $"ReqId_{Guid.NewGuid():N}"[..20];

        FakeAuthorizationService.Reset();
        FakeAuthorizationService.SetPermissions(userWithPermission, Permissions.ViewPersonalAndHealthData);
        FakeAuthorizationService.SetPermissions(userWithoutPermission);

        try
        {
            await SeedRequestorAsync(requestorId, requestorName);

            await SetRequestorInformationEnabledAsync(true);

            FakeCurrentUserService.CurrentUserId = userWithPermission;
            await AssertSearchContainsAsync(requestorName, SearchResultType.RequestorName);
            await AssertSearchContainsAsync(requestorId, SearchResultType.RequestorId);

            await SetRequestorInformationEnabledAsync(false);
            await AssertSearchDoesNotContainAsync(requestorName, SearchResultType.RequestorName);
            await AssertSearchDoesNotContainAsync(requestorId, SearchResultType.RequestorId);

            await SetRequestorInformationEnabledAsync(true);
            await AssertSearchContainsAsync(requestorName, SearchResultType.RequestorName);
            await AssertSearchContainsAsync(requestorId, SearchResultType.RequestorId);

            FakeCurrentUserService.CurrentUserId = userWithoutPermission;
            await AssertSearchDoesNotContainAsync(requestorName, SearchResultType.RequestorName);
            await AssertSearchDoesNotContainAsync(requestorId, SearchResultType.RequestorId);

            await WaitForBackgroundNotificationsAsync();
        }
        finally
        {
            FakeAuthorizationService.Reset();
            FakeCurrentUserService.CurrentUserId = Guid.NewGuid();
        }
    }

    private async Task WaitForBackgroundNotificationsAsync()
    {
        var backgroundNotificationHandler = Factory.Services.GetRequiredService<IBackgroundNotificationHandler>();
        await backgroundNotificationHandler.WaitForNotificationsAsync(TimeSpan.FromMinutes(3));
    }

    private async Task SeedRequestorAsync(string requestorId, string requestorName)
    {
        var requestDbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();

        requestDbContext.Requestors.Add(new Request.Domain.Requestor
        {
            RequestorId = requestorId,
            Name = requestorName
        });

        await requestDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task SetRequestorInformationEnabledAsync(bool isEnabled)
    {
        var settingsDbContext = ServiceProvider.GetRequiredService<ISettingsDbContext>();
        var configuration = ServiceProvider.GetRequiredService<IConfiguration>();

        var sectionPath = SettingsServiceCollectionExtensions.GetSectionPath<RequestSettings>(configuration);
        var key = $"{sectionPath}:{nameof(RequestSettings.IsRequestorInformationEnabled)}";

        var existing = await settingsDbContext.Settings
            .Where(x => x.Key == key)
            .ToListAsync(TestContext.Current.CancellationToken);

        settingsDbContext.Settings.RemoveRange(existing);
        settingsDbContext.Settings.Add(new Setting
        {
            Key = key,
            Value = isEnabled.ToString()
        });

        await settingsDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task AssertSearchContainsAsync(string searchTerm, SearchResultType expectedType)
    {
        var results = await SearchAsync(searchTerm);

        results.Should().Contain(x => x.ResultType == expectedType && string.Equals(x.Match, searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    private async Task AssertSearchDoesNotContainAsync(string searchTerm, SearchResultType expectedType)
    {
        var results = await SearchAsync(searchTerm);

        results.Should().NotContain(x => x.ResultType == expectedType && string.Equals(x.Match, searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<SearchResultDto[]> SearchAsync(string searchTerm)
    {
        var requestUri = new Uri(Factory.Server.BaseAddress!, $"api/search/searchResult?searchString={Uri.EscapeDataString(searchTerm)}");
        var results = await Factory.HttpClient.GetFromJsonAsync<SearchResultDto[]>(requestUri);

        return results ?? [];
    }
}
