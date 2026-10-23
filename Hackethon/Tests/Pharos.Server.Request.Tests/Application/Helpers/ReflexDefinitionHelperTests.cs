using Pharos.Server.Application.Reflex;
using Pharos.Server.Domain.Reflex;

namespace Pharos.Server.Request.Application.Helpers;

public class ReflexDefinitionHelperTests
{
    [Fact]
    [Trait("ModuleRequirement", "MR_5231")]
    public async Task CreateFreshReflexDefinitionsAsync_Should_RequestAllRequestors()
    {
        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(It.IsAny<GetReflexDefinitionsForTestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IReflexDefinition>());

        await ReflexDefinitionHelper.CreateFreshReflexDefinitionsAsync(
            senderMock.Object,
            "TestName",
            "MethodName",
            null,
            TestContext.Current.CancellationToken);

        senderMock.Verify(x => x.Send(
                It.Is<GetReflexDefinitionsForTestQuery>(q =>
                    q.TestName == "TestName" &&
                    q.MethodName == "MethodName"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5231")]
    public async Task CreateFreshReflexDefinitionsAsync_WhenIdsAreNull_ShouldCloneAllRequestorVariants()
    {
        var senderMock = new Mock<ISender>();
        var definitions = new List<IReflexDefinition>
        {
            CreateReflexDefinition(1, "Unrestricted"),
            CreateReflexDefinition(2, "Requestor1", "R1"),
            CreateReflexDefinition(3, "Requestor2", "R2")
        };

        senderMock
            .Setup(x => x.Send(It.IsAny<GetReflexDefinitionsForTestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(definitions);

        var result = await ReflexDefinitionHelper.CreateFreshReflexDefinitionsAsync(
            senderMock.Object,
            "TestName",
            "MethodName",
            null,
            TestContext.Current.CancellationToken);

        result.Should().HaveCount(3);
        result.Select(x => x.RequestorId).Should().BeEquivalentTo([null, "R1", "R2"]);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5231")]
    public async Task CreateFreshReflexDefinitionsAsync_ShouldPreserveRequestorIdOnFreshCopy()
    {
        var senderMock = new Mock<ISender>();
        var definition = CreateReflexDefinition(1, "RequestorReflex", "R1");

        senderMock
            .Setup(x => x.Send(It.IsAny<GetReflexDefinitionsForTestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([definition]);

        var result = await ReflexDefinitionHelper.CreateFreshReflexDefinitionsAsync(
            senderMock.Object,
            "TestName",
            "MethodName",
            null,
            TestContext.Current.CancellationToken);

        result.Single().RequestorId.Should().Be("R1");
        result.Single().Id.Should().Be(0);
    }

    [Fact]
    public async Task CreateFreshReflexDefinitionsAsync_Should_ReturnFilteredDefinitions_When_IdsMatch()
    {
        // Arrange
        var senderMock = new Mock<ISender>();

        var allReflexDefinitions = new List<IReflexDefinition>
        {
            CreateReflexDefinition(1, "Reflex 1"),
            CreateReflexDefinition(2, "Reflex 2"),
            CreateReflexDefinition(3, "Reflex 3")
        };

        senderMock.Setup(x => x.Send(It.IsAny<GetReflexDefinitionsForTestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allReflexDefinitions);

        var idsToFilter = new List<int> { 1, 3 };

        // Act
        var result = await ReflexDefinitionHelper.CreateFreshReflexDefinitionsAsync(
            senderMock.Object,
            "TestName",
            "MethodName",
            idsToFilter,
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Name == "Reflex 1");
        result.Should().Contain(r => r.Name == "Reflex 3");
        result.Should().NotContain(r => r.Name == "Reflex 2");
    }

    [Fact]
    public async Task CreateFreshReflexDefinitionsAsync_Should_CreateFreshCopiesWithoutIds()
    {
        // Arrange
        var senderMock = new Mock<ISender>();

        var reflexDefinition = CreateReflexDefinition(1, "Reflex 1");

        senderMock.Setup(x => x.Send(It.IsAny<GetReflexDefinitionsForTestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IReflexDefinition> { reflexDefinition });

        // Act
        var result = await ReflexDefinitionHelper.CreateFreshReflexDefinitionsAsync(
            senderMock.Object,
            "TestName",
            "MethodName",
            new List<int> { 1 },
            TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(0); // Fresh copy should not have an ID
        result[0].Name.Should().Be("Reflex 1");
    }

    [Fact]
    public async Task CreateFreshReflexDefinitionsAsync_Should_CopyReflexTests()
    {
        // Arrange
        var senderMock = new Mock<ISender>();

        var reflexDefinition = CreateReflexDefinition(1, "Reflex 1");

        senderMock.Setup(x => x.Send(It.IsAny<GetReflexDefinitionsForTestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IReflexDefinition> { reflexDefinition });

        // Act
        var result = await ReflexDefinitionHelper.CreateFreshReflexDefinitionsAsync(
            senderMock.Object,
            "TestName",
            "MethodName",
            new List<int> { 1 },
            TestContext.Current.CancellationToken);

        // Assert
        result[0].ReflexTests.Should().HaveCount(2);
        result[0].ReflexTests[0].TestName.Should().Be("FollowUpTest1");
        result[0].ReflexTests[1].TestName.Should().Be("FollowUpTest2");
    }

    [Fact]
    public async Task CreateFreshReflexDefinitionsAsync_Should_ReturnAllDefinitions_When_NoIdsMatch()
    {
        // Arrange
        var senderMock = new Mock<ISender>();

        var allReflexDefinitions = new List<IReflexDefinition>
        {
            CreateReflexDefinition(1, "Reflex 1"),
            CreateReflexDefinition(2, "Reflex 2")
        };

        senderMock.Setup(x => x.Send(It.IsAny<GetReflexDefinitionsForTestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allReflexDefinitions);

        var idsToFilter = new List<int> { 99, 100 };

        // Act
        var result = await ReflexDefinitionHelper.CreateFreshReflexDefinitionsAsync(
            senderMock.Object,
            "TestName",
            "MethodName",
            idsToFilter,
            TestContext.Current.CancellationToken);

        result.Should().HaveCount(0);
    }

    [Fact]
    public async Task CreateFreshReflexDefinitionsAsync_Should_CopyReflexCondition()
    {
        // Arrange
        var senderMock = new Mock<ISender>();

        var reflexDefinition = CreateReflexDefinition(1, "Reflex 1");

        senderMock.Setup(x => x.Send(It.IsAny<GetReflexDefinitionsForTestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IReflexDefinition> { reflexDefinition });

        // Act
        var result = await ReflexDefinitionHelper.CreateFreshReflexDefinitionsAsync(
            senderMock.Object,
            "TestName",
            "MethodName",
            new List<int> { 1 },
            TestContext.Current.CancellationToken);

        // Assert
        result[0].ReflexCondition.Should().NotBeNull();
        result[0].ReflexCondition.TestName.Should().Be("TestName");
        result[0].ReflexCondition.MethodName.Should().Be("MethodName");
        result[0].ReflexCondition.ResultType.Should().Be(ResultType.Concentration);
    }

    [Fact]
    public async Task CreateFreshReflexDefinitionsAsync_Should_PreserveAutomaticFlag()
    {
        // Arrange
        var senderMock = new Mock<ISender>();

        var reflexDefinition = CreateReflexDefinition(1, "Reflex 1");
        reflexDefinition.Automatic = false;

        senderMock.Setup(x => x.Send(It.IsAny<GetReflexDefinitionsForTestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IReflexDefinition> { reflexDefinition });

        // Act
        var result = await ReflexDefinitionHelper.CreateFreshReflexDefinitionsAsync(
            senderMock.Object,
            "TestName",
            "MethodName",
            new List<int> { 1 },
            TestContext.Current.CancellationToken);

        // Assert
        result[0].Automatic.Should().BeFalse();
    }

    private static ReflexDefinition CreateReflexDefinition(int id, string name, string? requestorId = null)
    {
        return new ReflexDefinition
        {
            Id = id,
            Name = name,
            Automatic = true,
            RequestorId = requestorId,
            ReflexCondition = new ReflexCondition
            {
                TestName = "TestName",
                MethodName = "MethodName",
                ResultType = ResultType.Concentration,
                CompareType = CompareType.Above,
                ConcentrationCompareValue = 3.5
            },
            ReflexTests = new List<ReflexTest>
            {
                new()
                {
                    TestName = "FollowUpTest1",
                    MethodName = "FollowUpMethod1",
                    NumberOfReplicates = 2,
                    InstrumentDilutionFactor = 1,
                    TestSortOrder = 1
                },
                new()
                {
                    TestName = "FollowUpTest2",
                    MethodName = "FollowUpMethod2",
                    NumberOfReplicates = 3,
                    InstrumentDilutionFactor = 2,
                    TestSortOrder = 2
                }
            }
        };
    }
}
