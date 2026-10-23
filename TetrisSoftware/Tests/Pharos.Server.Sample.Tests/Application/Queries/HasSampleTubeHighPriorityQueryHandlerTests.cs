using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Queries.HasSampleTubeHighPriority;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Queries;

public class HasSampleTubeHighPriorityQueryHandlerTests
{
    private readonly Mock<ISampleDbContext> _dbContextMock;
    private readonly InstrumentSampleStorage _sampleStorage = new();
    private readonly HasSampleTubeHighPriorityQueryHandler _sut;

    public HasSampleTubeHighPriorityQueryHandlerTests()
    {
        _dbContextMock = new Mock<ISampleDbContext>();
        _sut = new HasSampleTubeHighPriorityQueryHandler(_dbContextMock.Object, _sampleStorage);
    }

    [Theory]
    [InlineData(true, "2025-01-01T00:00:00+00:00", "2025-01-01T00:00:00+00:00", true)]
    [InlineData(false, "2025-01-01T00:00:00+00:00", "2025-01-01T00:00:00+00:00", false)]
    [InlineData(true, null, null, true)]
    [InlineData(false, null, null, false)]
    [InlineData(true, "2024-12-31T00:00:00+00:00", "2025-01-01T00:00:00+00:00", false)]
    public async Task Handle_ShouldReturnExpected_WhenTubesInStorageWithHighPriority_AndRequestCreatedDateVaries(
        bool highPriority,
        string? tubeRequestCreatedDate,
        string? queryRequestCreatedDate,
        bool expectedResult)
    {
        // Arrange
        var sampleId = "SAMPLE001";
        var parsedTubeRequestCreatedDate = tubeRequestCreatedDate is null
            ? (DateTimeOffset?)null
            : DateTimeOffset.Parse(tubeRequestCreatedDate, System.Globalization.CultureInfo.InvariantCulture);
        var parsedQueryRequestCreatedDate = queryRequestCreatedDate is null
            ? default
            : DateTimeOffset.Parse(queryRequestCreatedDate, System.Globalization.CultureInfo.InvariantCulture);

        var tube = new SampleTube
        {
            InstrumentNumber = "INSTR1",
            Area = SampleArea.SampleDrawer1,
            TrayPosition = 1,
            TubePosition = 1,
            SampleId = sampleId,
            RequestCreatedDate = parsedTubeRequestCreatedDate,
            HighPriority = highPriority
        };
        _sampleStorage.AddOrUpdateTube(tube, _ => tube);

        var query = new HasSampleTubeHighPriorityQuery(new RequestUid
        {
            SampleId = sampleId,
            RequestCreatedDate = parsedQueryRequestCreatedDate
        });

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_ShouldCheckDatabase_WhenStorageReturnsNull()
    {
        // Arrange
        var requestCreatedDate = DateTimeOffset.UtcNow;
        var sampleId = "SAMPLE001";

        var dbTubes = new List<SampleTube>
        {
            new() { SampleId = sampleId, RequestCreatedDate = requestCreatedDate, HighPriority = true }
        };

        _dbContextMock.Setup(x => x.SampleTubes).ReturnsDbSet(dbTubes);

        var query = new HasSampleTubeHighPriorityQuery(new RequestUid
        {
            SampleId = sampleId,
            RequestCreatedDate = requestCreatedDate
        });

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenDatabaseHasHighPriorityTube_WithRequestCreatedDate()
    {
        // Arrange
        var requestCreatedDate = DateTimeOffset.UtcNow;
        var sampleId = "SAMPLE001";

        var dbTubes = new List<SampleTube>
        {
            new() { SampleId = sampleId, RequestCreatedDate = requestCreatedDate, HighPriority = true },
            new() { SampleId = sampleId, RequestCreatedDate = requestCreatedDate, HighPriority = false }
        };

        _dbContextMock.Setup(x => x.SampleTubes).ReturnsDbSet(dbTubes);

        var query = new HasSampleTubeHighPriorityQuery(new RequestUid
        {
            SampleId = sampleId,
            RequestCreatedDate = requestCreatedDate
        });

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnTrue_WhenDatabaseHasHighPriorityTube_WithNullRequestCreatedDate()
    {
        // Arrange
        var sampleId = "SAMPLE001";

        var dbTubes = new List<SampleTube>
        {
            new() { SampleId = sampleId, RequestCreatedDate = null, HighPriority = true },
            new() { SampleId = sampleId, RequestCreatedDate = null, HighPriority = false }
        };

        _dbContextMock.Setup(x => x.SampleTubes).ReturnsDbSet(dbTubes);

        var query = new HasSampleTubeHighPriorityQuery(new RequestUid
        {
            SampleId = sampleId,
            RequestCreatedDate = default
        });

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenDatabaseHasHighPriorityTube_ButRequestCreatedDateDoesNotMatch()
    {
        // Arrange
        var requestCreatedDate = DateTimeOffset.UtcNow;
        var differentDate = DateTimeOffset.UtcNow.AddDays(-1);
        var sampleId = "SAMPLE001";

        var dbTubes = new List<SampleTube>
        {
            new() { SampleId = sampleId, RequestCreatedDate = differentDate, HighPriority = true },
            new() { SampleId = sampleId, RequestCreatedDate = requestCreatedDate, HighPriority = false }
        };

        _dbContextMock.Setup(x => x.SampleTubes).ReturnsDbSet(dbTubes);

        var query = new HasSampleTubeHighPriorityQuery(new RequestUid
        {
            SampleId = sampleId,
            RequestCreatedDate = requestCreatedDate
        });

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenDatabaseHasHighPriorityTube_WithNonNullRequestCreatedDate_ButQueryHasDefault()
    {
        // Arrange
        var differentDate = DateTimeOffset.UtcNow;
        var sampleId = "SAMPLE001";

        var dbTubes = new List<SampleTube>
        {
            new() { SampleId = sampleId, RequestCreatedDate = differentDate, HighPriority = true },
            new() { SampleId = sampleId, RequestCreatedDate = null, HighPriority = false }
        };

        _dbContextMock.Setup(x => x.SampleTubes).ReturnsDbSet(dbTubes);

        var query = new HasSampleTubeHighPriorityQuery(new RequestUid
        {
            SampleId = sampleId,
            RequestCreatedDate = default
        });

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenNoTubesExistAnywhere()
    {
        // Arrange
        var sampleId = "SAMPLE001";

        var dbTubes = new List<SampleTube>();

        _dbContextMock.Setup(x => x.SampleTubes).ReturnsDbSet(dbTubes);

        var query = new HasSampleTubeHighPriorityQuery(new RequestUid
        {
            SampleId = sampleId,
            RequestCreatedDate = DateTimeOffset.UtcNow
        });

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }
}
