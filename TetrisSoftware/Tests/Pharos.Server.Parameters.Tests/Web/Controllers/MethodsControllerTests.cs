using MediatR;
using Microsoft.AspNetCore.Authorization;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Queries;
using Pharos.Server.Parameters.Application.Commands.SpecializedMethodsAndTests;
using Pharos.Server.Parameters.Application.Queries.GetSpecializedMethodsAndTests;
using System.Reflection;

namespace Pharos.Server.Parameters.Web.Controllers;

public class MethodsControllerTests
{
    private readonly Mock<ISender> _senderMock = new();
    private readonly MethodsController _controller;

    public MethodsControllerTests()
    {
        _controller = new MethodsController(_senderMock.Object);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_340")]
    public async Task GetSpecializedMethodsAndTests_ShouldSendQueryAndReturnResult()
    {
        // Arrange
        var query = new GetSpecializedMethodsAndTestsQuery
        {
            Name = "ruo",
            Type = ["RUOTest"],
            StartIndex = 1,
            Count = 10
        };
        var expectedResult = new PartialQueryResult<SpecializedMethodOrTestDto>(
            [
                new SpecializedMethodOrTestDto
                {
                    MethodId = 1,
                    MethodTestId = 2,
                    Name = "RUO Test",
                    Type = SpecializedMethodOrTestType.RUOTest,
                    IsSpecializedMethodOrTestActivated = true
                }
            ],
            query.StartIndex,
            1);

        _senderMock
            .Setup(x => x.Send<PartialQueryResult<SpecializedMethodOrTestDto>>(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetSpecializedMethodsAndTests(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);
        _senderMock.Verify(x => x.Send<PartialQueryResult<SpecializedMethodOrTestDto>>(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_342")]
    public async Task SaveSpecializedMethodsAndTests_ShouldSendCommand()
    {
        // Arrange
        var command = new SaveSpecializedMethodsAndTestsCommand
        {
            Items =
            [
                new SaveSpecializedRequestDto
                {
                    MethodId = 1,
                    MethodTestId = 2,
                    Type = SpecializedMethodOrTestType.RUOTest,
                    IsSpecializedMethodOrTestActivated = false
                }
            ]
        };

        _senderMock
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _controller.SaveSpecializedMethodsAndTests(command, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [Trait("ProductRequirement", "PR_340")]
    [InlineData(nameof(MethodsController.GetSpecializedMethodsAndTests))]
    [InlineData(nameof(MethodsController.SaveSpecializedMethodsAndTests))]
    public void SpecializedMethodsAndTestsEndpoints_ShouldRequireEditServiceSettingsPermission(string methodName)
    {
        var method = typeof(MethodsController)
            .GetMethods()
            .Single(x => x.Name == methodName);

        method.GetCustomAttribute<AuthorizeAttribute>()!
            .Policy.Should().Be(Permissions.EditServiceSettings);
    }
}