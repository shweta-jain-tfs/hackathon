using Microsoft.Extensions.Options;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Reflex.Application.Queries.GetReflexDefinition;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Reflex.Interfaces;
using Xunit;

namespace Pharos.Server.Reflex.Application.Queries;

public class GetReflexDefinitionQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_ReflexDefinitionNotFound()
    {
        // Arrange
        var dbContextMock = new Mock<IReflexDbContext>();
        var senderMock = new Mock<MediatR.ISender>();
        dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet([]);

        var settingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        settingsMock.Setup(x => x.Value).Returns(new RequestSettings { IsRequestorInformationEnabled = true });

        var authServiceMock = new Mock<IAuthorizationService>();
        authServiceMock.Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData)).ReturnsAsync(true);
        var sut = new GetReflexDefinitionQueryHandler(dbContextMock.Object, senderMock.Object, new Mock<IMethodsService>().Object, new Mock<IDateTime>().Object, settingsMock.Object, authServiceMock.Object);
        var query = new GetReflexDefinitionQuery(999);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(query, TestContext.Current.CancellationToken));
    }
}
