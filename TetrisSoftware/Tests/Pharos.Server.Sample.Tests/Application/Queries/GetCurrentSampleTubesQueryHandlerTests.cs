using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Queries.GetSampleTubes;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Queries;

public class GetCurrentSampleTubesQueryHandlerTests
{
    private readonly Mock<ISampleDbContext> _dbContextMock = new();
    private readonly InstrumentSampleStorage _sampleStorage = new();

    [Trait("ProductRequirement", "PR_2210")]
    [Trait("ModuleRequirement", "MR_5732")]
    [Fact]
    public async Task Handle_ShouldReturnOnlyMatchingStorageTubeAndIgnoreDbHistory_WhenSampleIdIsReused()
    {
        // Arrange
        const string sampleId = "SAMPLE001";
        var requestedDate = DateTimeOffset.Parse("2026-01-01T10:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture);
        var otherDate = DateTimeOffset.Parse("2026-01-02T10:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture);

        AddStorageTube("STORAGE_REQUESTED", sampleId, requestedDate, trayId: "TRAY01", tubePosition: 1);
        AddStorageTube("STORAGE_OTHER", sampleId, otherDate, trayId: "TRAY02", tubePosition: 2);

        var dbTubes = new List<SampleTube>
        {
            CreateDbTube("DB_REQUESTED", sampleId, requestedDate, trayId: "TRAY03", tubePosition: 3),
            CreateDbTube("DB_OTHER", sampleId, otherDate, trayId: "TRAY04", tubePosition: 4)
        };

        _dbContextMock
            .Setup(x => x.SampleTubes)
            .ReturnsDbSet(dbTubes);

        var sut = new GetRequestListSampleTubesQueryHandler(_sampleStorage, _dbContextMock.Object);

        var query = CreateLastTrayQuery([
            new RequestUid
            {
                SampleId = sampleId,
                RequestCreatedDate = requestedDate
            }
        ]);

        // Act
        var result = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var tubes = result.Should().ContainSingle().Subject.Tubes;
        tubes.Should().ContainSingle();
        tubes.Should().Contain(t => t.TrayId == "TRAY01" && t.TubePosition == 1);
        tubes.Should().NotContain(t => t.TrayId == "TRAY02" || t.TrayId == "TRAY03" || t.TrayId == "TRAY04");
    }

    [Fact]
    public async Task Handle_ShouldUseExactRequestCreatedDateByDefault_WhenSampleIdIsReused()
    {
        // Arrange
        const string sampleId = "SAMPLE001";
        var requestedDate = DateTimeOffset.Parse("2026-01-01T10:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture);
        var otherDate = DateTimeOffset.Parse("2026-01-02T10:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture);

        AddStorageTube("STORAGE_REQUESTED", sampleId, requestedDate, trayId: "TRAY01", tubePosition: 1);
        AddStorageTube("STORAGE_OTHER", sampleId, otherDate, trayId: "TRAY02", tubePosition: 2);

        _dbContextMock
            .Setup(x => x.SampleTubes)
            .ReturnsDbSet([]);

        var sut = new GetCurrentSampleTubesQueryHandler(_sampleStorage, _dbContextMock.Object);

        var query = new GetCurrentSampleTubesQuery([
            new RequestUid
            {
                SampleId = sampleId,
                RequestCreatedDate = requestedDate
            }
        ]);

        // Act
        var result = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var tubes = result.Should().ContainSingle().Subject.Tubes;
        tubes.Should().ContainSingle();
        tubes[0].TrayId.Should().Be("TRAY01");
        tubes[0].TubePosition.Should().Be(1);
    }

    [Trait("ProductRequirement", "PR_2210")]
    [Trait("ModuleRequirement", "MR_5732")]
    [Fact]
    public async Task Handle_ShouldReturnStorageTubeWithoutRequestCreatedDate_WhenSampleIdMatchesSingleRequestedRequest()
    {
        // Arrange
        const string sampleId = "SAMPLE001";
        var requestedDate = DateTimeOffset.Parse("2026-01-01T10:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture);

        AddStorageTube("STORAGE_UNASSIGNED", sampleId, requestCreatedDate: null, trayId: "TRAY01", tubePosition: 1);

        _dbContextMock
            .Setup(x => x.SampleTubes)
            .ReturnsDbSet([]);

        var sut = new GetRequestListSampleTubesQueryHandler(_sampleStorage, _dbContextMock.Object);

        var query = CreateLastTrayQuery([
            new RequestUid
            {
                SampleId = sampleId,
                RequestCreatedDate = requestedDate
            }
        ]);

        // Act
        var result = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var tubes = result.Should().ContainSingle().Subject.Tubes;
        tubes.Should().ContainSingle();
        tubes[0].TrayId.Should().Be("TRAY01");
        tubes[0].TubePosition.Should().Be(1);
    }

    [Trait("ProductRequirement", "PR_2210")]
    [Trait("ModuleRequirement", "MR_5732")]
    [Fact]
    public async Task Handle_ShouldReturnStorageOnlyForMatchingRequestDate_AndKeepDbFallbackForOtherRequest_WhenSampleIdIsReused()
    {
        // Arrange
        const string sampleId = "SAMPLE001";
        var requestedDate = DateTimeOffset.Parse("2026-01-01T10:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture);
        var otherDate = DateTimeOffset.Parse("2026-01-02T10:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture);

        AddStorageTube("STORAGE_REQUESTED", sampleId, requestedDate, trayId: "TRAY01", tubePosition: 1);

        var dbTubes = new List<SampleTube>
        {
            CreateDbTube("DB_REQUESTED", sampleId, requestedDate, trayId: "TRAY02", tubePosition: 2, instrumentNumber: "INSTR1"),
            CreateDbTube("DB_OTHER", sampleId, otherDate, trayId: "TRAY03", tubePosition: 3, instrumentNumber: "INSTR1")
        };

        _dbContextMock
            .Setup(x => x.SampleTubes)
            .ReturnsDbSet(dbTubes);

        var sut = new GetRequestListSampleTubesQueryHandler(_sampleStorage, _dbContextMock.Object);

        var query = CreateLastTrayQuery([
            new RequestUid
            {
                SampleId = sampleId,
                RequestCreatedDate = requestedDate
            },
            new RequestUid
            {
                SampleId = sampleId,
                RequestCreatedDate = otherDate
            }
        ]);

        // Act
        var result = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);

        var requestedResult = result.Single(x => x.RequestUid.RequestCreatedDate == requestedDate);
        requestedResult.Tubes.Should().ContainSingle();
        requestedResult.Tubes[0].TrayId.Should().Be("TRAY01");
        requestedResult.Tubes[0].TubePosition.Should().Be(1);

        var otherResult = result.Single(x => x.RequestUid.RequestCreatedDate == otherDate);
        otherResult.Tubes.Should().ContainSingle();
        otherResult.Tubes[0].TrayId.Should().Be("TRAY03");
        otherResult.Tubes[0].TubePosition.Should().Be(3);
    }

    [Trait("ProductRequirement", "PR_2210")]
    [Trait("ModuleRequirement", "MR_5732")]
    [Fact]
    public async Task Handle_ShouldReturnPersistedTubeInInstrument_WhenStorageIsEmptyAfterRestart()
    {
        // Arrange
        const string sampleId = "SAMPLE001";
        var requestedDate = DateTimeOffset.Parse("2026-01-01T10:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture);

        var dbTubes = new List<SampleTube>
        {
            CreateDbTube("DB_LOADED", sampleId, requestedDate, trayId: "TRAY01", tubePosition: 1, instrumentNumber: "INSTR1")
        };

        _dbContextMock
            .Setup(x => x.SampleTubes)
            .ReturnsDbSet(dbTubes);

        var sut = new GetRequestListSampleTubesQueryHandler(_sampleStorage, _dbContextMock.Object);

        var query = CreateLastTrayQuery([
            new RequestUid
            {
                SampleId = sampleId,
                RequestCreatedDate = requestedDate
            }
        ]);

        // Act
        var result = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var tubes = result.Should().ContainSingle().Subject.Tubes;
        tubes.Should().ContainSingle();
        tubes[0].TrayId.Should().Be("TRAY01");
        tubes[0].TubePosition.Should().Be(1);
    }

    [Trait("ProductRequirement", "PR_2210")]
    [Trait("ModuleRequirement", "MR_5732")]
    [Fact]
    public async Task Handle_ShouldReturnLatestPersistedTubeLocation_WhenSameTubeWasMoved()
    {
        // Arrange
        const string sampleId = "SAMPLE001";
        var requestedDate = DateTimeOffset.Parse("2026-01-01T10:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture);

        var dbTubes = new List<SampleTube>
        {
            CreateDbTube(
                "TUBE001",
                sampleId,
                requestedDate,
                trayId: "7771",
                tubePosition: 2,
                instrumentNumber: "INSTR1",
                loadTimestamp: DateTimeOffset.Parse("2026-09-14T07:20:00+00:00", System.Globalization.CultureInfo.InvariantCulture),
                id: 1),
            CreateDbTube(
                "TUBE001",
                sampleId,
                requestedDate,
                trayId: "1111",
                tubePosition: 3,
                instrumentNumber: "INSTR1",
                loadTimestamp: DateTimeOffset.Parse("2026-09-14T08:42:00+00:00", System.Globalization.CultureInfo.InvariantCulture),
                id: 2)
        };

        _dbContextMock
            .Setup(x => x.SampleTubes)
            .ReturnsDbSet(dbTubes);

        var sut = new GetRequestListSampleTubesQueryHandler(_sampleStorage, _dbContextMock.Object);

        var query = CreateLastTrayQuery([
            new RequestUid
            {
                SampleId = sampleId,
                RequestCreatedDate = requestedDate
            }
        ]);

        // Act
        var result = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var tubes = result.Should().ContainSingle().Subject.Tubes;
        tubes.Should().ContainSingle();
        tubes[0].TrayId.Should().Be("1111");
        tubes[0].TubePosition.Should().Be(3);
        tubes[0].TubePositionDisplayValue.Should().Be("A3");
    }

    [Trait("ProductRequirement", "PR_2210")]
    [Trait("ModuleRequirement", "MR_5732")]
    [Fact]
    public async Task Handle_ShouldPreferStorageTubeOverPersistedHistory_WhenTubeWasMoved()
    {
        // Arrange
        const string sampleId = "SAMPLE001";
        var requestedDate = DateTimeOffset.Parse("2026-01-01T10:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture);

        AddStorageTube("CURRENT_TUBE", sampleId, requestedDate, trayId: "1111", tubePosition: 3);

        var dbTubes = new List<SampleTube>
        {
            CreateDbTube(
                "OLD_TUBE",
                sampleId,
                requestedDate,
                trayId: "7771",
                tubePosition: 2,
                instrumentNumber: "INSTR1",
                loadTimestamp: DateTimeOffset.Parse("2026-09-14T07:20:00+00:00", System.Globalization.CultureInfo.InvariantCulture),
                id: 1)
        };

        _dbContextMock
            .Setup(x => x.SampleTubes)
            .ReturnsDbSet(dbTubes);

        var sut = new GetRequestListSampleTubesQueryHandler(_sampleStorage, _dbContextMock.Object);

        var query = CreateLastTrayQuery([
            new RequestUid
            {
                SampleId = sampleId,
                RequestCreatedDate = requestedDate
            }
        ]);

        // Act
        var result = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var tubes = result.Should().ContainSingle().Subject.Tubes;
        tubes.Should().ContainSingle();
        tubes[0].TrayId.Should().Be("1111");
        tubes[0].TubePosition.Should().Be(3);
        tubes[0].TubePositionDisplayValue.Should().Be("A3");
    }

    [Trait("ProductRequirement", "PR_2210")]
    [Trait("ModuleRequirement", "MR_5732")]
    [Fact]
    public async Task Handle_ShouldNotAssignStorageTube_WhenRequestCreatedDateIsMissingAndSampleIdMatchesMultipleRequestedRequests()
    {
        // Arrange
        const string sampleId = "SAMPLE001";
        var requestedDate = DateTimeOffset.Parse("2026-01-01T10:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture);
        var otherDate = DateTimeOffset.Parse("2026-01-02T10:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture);

        AddStorageTube("STORAGE_UNASSIGNED", sampleId, requestCreatedDate: null, trayId: "TRAY01", tubePosition: 1);

        _dbContextMock
            .Setup(x => x.SampleTubes)
            .ReturnsDbSet([]);

        var sut = new GetRequestListSampleTubesQueryHandler(_sampleStorage, _dbContextMock.Object);

        var query = CreateLastTrayQuery([
            new RequestUid
            {
                SampleId = sampleId,
                RequestCreatedDate = requestedDate
            },
            new RequestUid
            {
                SampleId = sampleId,
                RequestCreatedDate = otherDate
            }
        ]);

        // Act
        var result = await sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(x => x.Tubes.Count == 0);
    }

    private void AddStorageTube(
        string tubeId,
        string sampleId,
        DateTimeOffset? requestCreatedDate,
        string trayId,
        int tubePosition)
    {
        var tube = new SampleTube
        {
            TubeId = tubeId,
            InstrumentNumber = "INSTR1",
            Area = SampleArea.SampleDrawer1,
            TrayPosition = 1,
            TubePosition = tubePosition,
            TrayId = trayId,
            SampleId = sampleId,
            RequestCreatedDate = requestCreatedDate,
            InstrumentError = SampleTubeError.None
        };

        _sampleStorage.AddOrUpdateTube(tube, _ => tube);
    }

    private static GetRequestListSampleTubesQuery CreateLastTrayQuery(IReadOnlyCollection<RequestUid> requestUids)
    {
        return new GetRequestListSampleTubesQuery(requestUids);
    }

    private static SampleTube CreateDbTube(
        string tubeId,
        string sampleId,
        DateTimeOffset requestCreatedDate,
        string trayId,
        int tubePosition,
        string? instrumentNumber = null,
        DateTimeOffset? loadTimestamp = null,
        int id = 0)
    {
        return new SampleTube
        {
            Id = id,
            TubeId = tubeId,
            InstrumentNumber = instrumentNumber,
            TubePosition = tubePosition,
            TrayId = trayId,
            SampleId = sampleId,
            RequestCreatedDate = requestCreatedDate,
            LoadTimestamp = loadTimestamp
        };
    }
}
