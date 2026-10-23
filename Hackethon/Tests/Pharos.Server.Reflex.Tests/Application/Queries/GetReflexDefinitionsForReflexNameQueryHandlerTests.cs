using Microsoft.Extensions.Options;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Reflex;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Reflex.Domain;
using Pharos.Server.Reflex.Interfaces;
using Xunit;

namespace Pharos.Server.Reflex.Application.Queries;

public class GetReflexDefinitionsForReflexNameQueryHandlerTests
{
    [Fact]
    [Trait("ModuleRequirement", "MR_5697")]
    [Trait("ProductRequirement", "PR_1798")]
    public async Task Handle_Should_ReturnNull_When_ConditionTestDoesNotMatch()
    {
        // Arrange
        var dbContextMock = new Mock<IReflexDbContext>();
        dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(
        [
            CreateReflexDefinition("Reflexdef", "fx1", "slgE")
        ]);

        var sut = new GetReflexDefinitionsForReflexNameQueryHandler(dbContextMock.Object, CreateRequestSettingsMock().Object);

        // Act
        var result = await sut.Handle(new GetReflexDefinitionsForReflexNameQuery("Reflexdef", "f20", "slgE", null), TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5867")]
    [Trait("ProductRequirement", "PR_1798")]
    public async Task Handle_Should_ReturnDefinition_When_NameAndConditionTestMatch()
    {
        // Arrange
        var dbContextMock = new Mock<IReflexDbContext>();
        dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(
        [
            CreateReflexDefinition("Reflexdef", "fx1", "slgE")
        ]);

        var sut = new GetReflexDefinitionsForReflexNameQueryHandler(dbContextMock.Object, CreateRequestSettingsMock().Object);

        // Act
        var result = await sut.Handle(new GetReflexDefinitionsForReflexNameQuery("Reflexdef", "fx1", "slgE", null), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Reflexdef", result.Name);
    }

    private static Mock<IOptionsSnapshot<RequestSettings>> CreateRequestSettingsMock()
    {
        var mock = new Mock<IOptionsSnapshot<RequestSettings>>();
        mock.Setup(x => x.Value).Returns(new RequestSettings
        {
            ReflexTestingEnabled = true
        });
        return mock;
    }

    private static ReflexDefinition CreateReflexDefinition(string name, string conditionTestName, string conditionMethodName)
    {
        return new ReflexDefinition
        {
            Id = 1,
            Name = name,
            Automatic = true,
            Active = true,
            ReflexCondition = new ReflexCondition
            {
                TestName = conditionTestName,
                MethodName = conditionMethodName,
                ResultType = ResultType.CutOff,
                CompareType = CompareType.EqualTo,
                CutOffCompareValue = "Positive"
            },
            ReflexTests = []
        };
    }
}
