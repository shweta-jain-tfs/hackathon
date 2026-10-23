using System.Text.Json;
using MediatR;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Queries.GetSampleTubes;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Queries;

public class GetUnloadableQualityControlTubesQueryHandlerTests
{
    private readonly Mock<ICurrentInstrumentService> _currentInstrument = new();
    private readonly Mock<IInstrumentSampleStorage> _storage = new();
    private readonly Mock<ISampleDbContext> _dbContext = new();
    private readonly Mock<IInstrumentManager> _instruments = new();
    private readonly Mock<ISender> _sender = new();

    public GetUnloadableQualityControlTubesQueryHandlerTests()
    {
        _currentInstrument.SetupGet(x => x.InstrumentNumber).Returns("123");
        _dbContext.Setup(x => x.SampleTrays).ReturnsDbSet(new List<SampleTray>());
        var instrument = new Mock<IInstrument>();
        instrument.SetupGet(x => x.DisplayName).Returns("Instrument 1");
        _instruments.Setup(x => x.FindInstrument("123")).Returns(instrument.Object);
        _sender.Setup(x => x.Send(It.IsAny<IsQualityControlQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlResultDto(true, "QC1", false, "LOT1", 1));
    }

    [Fact]
    public async Task Handle_MapsLiveTubeDetailsAndQualityControlLot()
    {
        var tube = CreateTube();
        tube.TrayId = "TRAY1";
        tube.IdentitySource = SampleTubeIdentitySource.ManualInput;
        tube.Status = SampleTubeStatus.PipettedQc;
        tube.InternalError = SampleTubeError.PipettingError;
        _storage.Setup(x => x.GetTubesBySampleId("QC1", true)).Returns(new List<SampleTube> { tube });
        _storage.Setup(x => x.IsTrayInAnyInstrument("TRAY1")).Returns(true);

        var result = await CreateHandler().Handle(new GetUnloadableQualityControlTubesQuery("QC1"), TestContext.Current.CancellationToken);

        var item = result.Should().ContainSingle().Subject;
        item.Id.Should().Be("TUBE1");
        item.Lot.Should().Be("LOT1");
        item.DetectedDateTime.Should().Be(tube.LoadTimestamp.GetValueOrDefault());
        item.InstrumentNumber.Should().Be("123");
        item.InstrumentName.Should().Be("Instrument 1");
        item.Area.Should().Be(SampleArea.InternalStorage);
        item.TubeStatus.Should().Be(SampleTubeStatus.PipettedQc);
        item.Error.Should().Be(SampleTubeError.PipettingError);
        item.UnloadRequested.Should().BeFalse();
        item.ManuallyEntered.Should().BeTrue();
        item.TrayId.Should().Be("TRAY1");
        item.IsInCurrentInstrument.Should().BeTrue();
        item.DoesTrayExist.Should().BeTrue();
        item.TubePosition.Should().Be(12);
        item.TubePositionDisplayValue.Should().Be("B2");
        item.RemainingVolume.Should().Be(100);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(item, JsonSerializerOptions.Web));
        json.RootElement.GetProperty("instrumentName").GetString().Should().Be("Instrument 1");
        json.RootElement.TryGetProperty("tube", out _).Should().BeFalse();
        json.RootElement.TryGetProperty("imageIdentity", out _).Should().BeFalse();
        _sender.Verify(x => x.Send(new IsQualityControlQuery("barcode", "123"), TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyRequestedQualityControlFromLiveStorage()
    {
        _storage.Setup(x => x.GetTubesBySampleId("Missing QC", true)).Returns(new List<SampleTube>());

        var result = await CreateHandler().Handle(new GetUnloadableQualityControlTubesQuery("Missing QC"), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
        _dbContext.VerifyGet(x => x.SampleTubes, Times.Never);
    }

    [Fact]
    public async Task Handle_ChecksPersistedTrayAndMarksOtherInstrument()
    {
        var tube = CreateTube();
        tube.TrayId = "TRAY1";
        _storage.Setup(x => x.GetTubesBySampleId("QC1", true)).Returns(new List<SampleTube> { tube });
        _currentInstrument.SetupGet(x => x.InstrumentNumber).Returns("456");
        _dbContext.Setup(x => x.SampleTrays).ReturnsDbSet(new List<SampleTray> { new() { Id = "TRAY1" } });

        var result = await CreateHandler().Handle(new GetUnloadableQualityControlTubesQuery("QC1"), TestContext.Current.CancellationToken);

        var item = result.Should().ContainSingle().Subject;
        item.DoesTrayExist.Should().BeTrue();
        item.IsInCurrentInstrument.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, null)]
    [InlineData(1, "A1")]
    [InlineData(10, "A10")]
    [InlineData(11, "B1")]
    [InlineData(50, "E10")]
    [InlineData(51, null)]
    public async Task Handle_MapsTubePosition(int? position, string? expectedDisplayValue)
    {
        var tube = CreateTube();
        tube.TubePosition = position;
        _storage.Setup(x => x.GetTubesBySampleId("QC1", true)).Returns(new List<SampleTube> { tube });

        var result = await CreateHandler().Handle(new GetUnloadableQualityControlTubesQuery("QC1"), TestContext.Current.CancellationToken);

        var item = result.Should().ContainSingle().Subject;
        item.TubePosition.Should().Be(position);
        item.TubePositionDisplayValue.Should().Be(expectedDisplayValue);
    }

    [Fact]
    public async Task Handle_DefaultsMissingTimestampAndLotWithoutBarcode()
    {
        var tube = CreateTube();
        tube.LoadTimestamp = null;
        tube.TubeBarcode = null;
        _storage.Setup(x => x.GetTubesBySampleId("QC1", true)).Returns(new List<SampleTube> { tube });

        var result = await CreateHandler().Handle(new GetUnloadableQualityControlTubesQuery("QC1"), TestContext.Current.CancellationToken);

        var item = result.Should().ContainSingle().Subject;
        item.DetectedDateTime.Should().Be(default(DateTimeOffset));
        item.Lot.Should().BeEmpty();
        item.ManuallyEntered.Should().BeFalse();
        item.DoesTrayExist.Should().BeFalse();
        _sender.Verify(x => x.Send(It.IsAny<IsQualityControlQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private GetUnloadableQualityControlTubesQueryHandler CreateHandler() => new(
        _currentInstrument.Object, _storage.Object, _dbContext.Object, _instruments.Object, _sender.Object);

    private static SampleTube CreateTube() => new()
    {
        TubeId = "TUBE1",
        SampleId = "QC1",
        SampleType = SampleType.QualityControl,
        TubeBarcode = "barcode",
        InstrumentNumber = "123",
        Area = SampleArea.InternalStorage,
        TubePosition = 12,
        LoadTimestamp = DateTimeOffset.UtcNow,
        SampleVolume = 100,
        ImageIdentity = "image"
    };
}
