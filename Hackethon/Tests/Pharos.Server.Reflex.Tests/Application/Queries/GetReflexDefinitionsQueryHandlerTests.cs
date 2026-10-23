using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Reflex.Application.Queries.GetReflexDefinitions;
using Pharos.Server.Reflex.Domain;
using Pharos.Server.Reflex.Interfaces;
using Xunit;

namespace Pharos.Server.Reflex.Application.Queries;

public class GetReflexDefinitionsQueryHandlerTests
{
    private readonly Mock<IReflexDbContext> _dbContextMock;
    private readonly GetReflexDefinitionsQueryHandler _sut;

    public GetReflexDefinitionsQueryHandlerTests()
    {
        _dbContextMock = new Mock<IReflexDbContext>();

        var settingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        settingsMock.Setup(x => x.Value).Returns(new RequestSettings { IsRequestorInformationEnabled = true });

        var authServiceMock = new Mock<IAuthorizationService>();
        authServiceMock.Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData)).ReturnsAsync(true);

        _sut = new GetReflexDefinitionsQueryHandler(_dbContextMock.Object, settingsMock.Object, authServiceMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnAllReflexDefinitions_When_NoFiltersApplied()
    {
        // Arrange
        var reflexDefinitions = new List<ReflexDefinition>
        {
            CreateReflexDefinition(1, "Reflex 1", true, true, null),
            CreateReflexDefinition(2, "Reflex 2", false, true, null),
            CreateReflexDefinition(3, "Reflex 3", true, false, null)
        };

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);

        var query = new GetReflexDefinitionsQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_Should_FilterByName_When_NameProvided()
    {
        // Arrange
        var reflexDefinitions = new List<ReflexDefinition>
        {
            CreateReflexDefinition(1, "High IgE Reflex", true, true, null),
            CreateReflexDefinition(2, "Low IgE Reflex", false, true, null),
            CreateReflexDefinition(3, "Allergy Panel", true, false, null)
        };

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);

        var query = new GetReflexDefinitionsQuery
        {
            Name = "IgE",
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(item => item.Name.Should().Contain("IgE"));
    }

    [Fact]
    public async Task Handle_Should_FilterByRequestorId_When_RequestorIdProvided()
    {
        // Arrange
        var reflexDefinitions = new List<ReflexDefinition>
        {
            CreateReflexDefinition(1, "Reflex 1", true, true, "REQ001"),
            CreateReflexDefinition(2, "Reflex 2", false, true, "REQ002"),
            CreateReflexDefinition(3, "Reflex 3", true, false, null)
        };

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);

        var query = new GetReflexDefinitionsQuery
        {
            RequestorId = "REQ001",
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().RequestorId.Should().Be("REQ001");
    }

    [Fact]
    public async Task Handle_Should_SortByName_When_SortPropertyIsName()
    {
        // Arrange
        var reflexDefinitions = new List<ReflexDefinition>
        {
            CreateReflexDefinition(1, "Zebra Reflex", true, true, null),
            CreateReflexDefinition(2, "Alpha Reflex", false, true, null),
            CreateReflexDefinition(3, "Beta Reflex", true, false, null)
        };

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);

        var query = new GetReflexDefinitionsQuery
        {
            SortByProperty = nameof(ReflexDefinitionSummaryDto.Name),
            SortByAscending = true,
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.Items.First().Name.Should().Be("Alpha Reflex");
        result.Items.Last().Name.Should().Be("Zebra Reflex");
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyList_When_NoMatchingReflexDefinitions()
    {
        // Arrange
        var reflexDefinitions = new List<ReflexDefinition>();
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);

        var query = new GetReflexDefinitionsQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ApplyPaging_When_StartIndexAndCountProvided()
    {
        // Arrange
        var reflexDefinitions = new List<ReflexDefinition>
        {
            CreateReflexDefinition(1, "Reflex 1", true, true, null),
            CreateReflexDefinition(2, "Reflex 2", false, true, null),
            CreateReflexDefinition(3, "Reflex 3", true, false, null),
            CreateReflexDefinition(4, "Reflex 4", true, true, null),
            CreateReflexDefinition(5, "Reflex 5", false, false, null)
        };

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);

        var query = new GetReflexDefinitionsQuery
        {
            StartIndex = 2,
            Count = 2
        };

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
    }

    private static ReflexDefinition CreateReflexDefinition(int id, string name, bool automatic, bool active, string? requestorId)
    {
        return new ReflexDefinition
        {
            Id = id,
            Name = name,
            Automatic = automatic,
            Active = active,
            RequestorId = requestorId,
            ReflexCondition = new ReflexCondition
            {
                TestName = "TestName",
                MethodName = "MethodName",
                ResultType = ResultType.Concentration,
                CompareType = CompareType.Above,
                ConcentrationCompareValue = 1.0
            },
            ReflexTests = new List<ReflexTest>
            {
                new()
                {
                    TestName = "FollowUpTest",
                    MethodName = "FollowUpMethod",
                    NumberOfReplicates = 2,
                    InstrumentDilutionFactor = 1
                }
            },
            Created = DateTimeOffset.Now.AddDays(-id),
            LastModified = DateTimeOffset.Now.AddDays(-id)
        };
    }
}
