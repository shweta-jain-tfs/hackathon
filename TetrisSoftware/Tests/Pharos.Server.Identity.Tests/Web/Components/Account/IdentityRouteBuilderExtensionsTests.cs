using System.Net;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;
using Pharos.Server.Identity.Domain;

namespace Pharos.Server.Identity.Web.Components.Account;

public class IdentityRouteBuilderExtensionsTests
{
    [Theory]
    [InlineData("/Account/Login", "/Account/Login")]
    [InlineData("lockscreen", "/lockscreen")]
    [InlineData("", "/")]
    [InlineData(null, "/")]
    public async Task Logout_RedirectsToLocalReturnUrlAndPublishesLogout(string? returnUrl, string expectedLocation)
    {
        var signInManager = CreateSignInManagerMock();
        signInManager.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);
        var publisher = new Mock<IPublisher>();
        publisher.Setup(x => x.Publish(It.IsAny<UserLoggedOutEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(signInManager.Object);
        builder.Services.AddSingleton(Mock.Of<ICurrentInstrumentService>());
        builder.Services.AddSingleton(publisher.Object);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddOptions<BearerTokenOptions>();
        builder.Services.AddAntiforgery();
        await using var app = builder.Build();
        app.UseAntiforgery();
        app.MapAdditionalIdentityEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = app.GetTestClient();
        var url = returnUrl is null ? "/Account/Logout" : $"/Account/Logout?returnUrl={Uri.EscapeDataString(returnUrl)}";

        using var response = await client.GetAsync(new Uri(url, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Be(expectedLocation);
        signInManager.Verify(x => x.SignOutAsync(), Times.Once);
        publisher.Verify(x => x.Publish(It.IsAny<UserLoggedOutEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock()
    {
        var userManager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);
        return new Mock<SignInManager<ApplicationUser>>(userManager.Object, Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(), Options.Create(new IdentityOptions()),
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(), Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<ApplicationUser>>());
    }
}
