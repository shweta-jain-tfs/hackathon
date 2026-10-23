using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Sample.Application.Services;

namespace Pharos.Server.Sample.Tests.Application.Services;

[Collection(nameof(SampleTestsCollection))]
public class SampleQcIdProviderTests
{
    private readonly Mock<IOptionsSnapshot<SpecialSampleHandlingSettings>> _settingsMock;
    private readonly Mock<IDateTime> _dateTimeMock;
    private readonly Mock<ISender> _senderMock;

    public SampleQcIdProviderTests()
    {
        _settingsMock = new Mock<IOptionsSnapshot<SpecialSampleHandlingSettings>>();
        _dateTimeMock = new Mock<IDateTime>();
        _senderMock = new Mock<ISender>();

        _senderMock.Setup(x => x.Send(It.IsAny<IsQualityControlQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(QualityControlResultDto.NotQualityControl);
    }

    private SampleQcIdProvider GetSut()
    {
        return new SampleQcIdProvider(_settingsMock.Object, _dateTimeMock.Object, _senderMock.Object);
    }

    [Fact]
    public async Task GetSampleQCTubeKey_When_BarcodeIs123_AndNotUsingSampleTubesWithSameSampleId_ShouldReturnSameTubeId()
    {
        var settings = new SpecialSampleHandlingSettings
        {
            ExtractSampleIdFromBarcode = false,
            ExtractSampleIdSuffixFromBarcode = false,
            SampleTubesWithSameSampleId = false
        };
        _settingsMock.Setup(x => x.Value).Returns(settings);
        var date = new DateTime(2025, 03, 28, 8, 25, 0);
        _dateTimeMock.Setup(x => x.Now).Returns(() => new DateTimeOffset(date, TimeSpan.Zero));

        var tubeBarcode = "123";

        var sut = GetSut();
        var firstTube = await sut.GetSampleQcTubeKey(tubeBarcode);

        date = date.AddTicks(1);

        var secondTube = await sut.GetSampleQcTubeKey(tubeBarcode);

        firstTube.Barcode.Should().Be("123");
        firstTube.SampleType.Should().Be(SampleType.Sample);
        firstTube.Id.Should().Be("123");
        firstTube.IdSuffix.Should().Be(string.Empty);
        firstTube.TubeId.Should().Be("123");
        firstTube.UnreadableBarcode.Should().BeFalse();

        secondTube.Barcode.Should().Be("123");
        secondTube.SampleType.Should().Be(SampleType.Sample);
        secondTube.Id.Should().Be("123");
        secondTube.IdSuffix.Should().Be(string.Empty);
        secondTube.TubeId.Should().Be("123");
        secondTube.UnreadableBarcode.Should().BeFalse();
    }

    [Fact]
    public async Task GetSampleQCTubeKey_When_BarcodeIs123_AndUsingSampleTubesWithSameSampleId_ShouldReturnUniqueTubeIds()
    {
        var settings = new SpecialSampleHandlingSettings
        {
            ExtractSampleIdFromBarcode = false,
            ExtractSampleIdSuffixFromBarcode = false,
            SampleTubesWithSameSampleId = true
        };
        _settingsMock.Setup(x => x.Value).Returns(settings);
        var date = new DateTime(2025, 03, 28, 8, 25, 0);
        _dateTimeMock.Setup(x => x.Now).Returns(() => new DateTimeOffset(date, TimeSpan.Zero));

        var tubeBarcode = "123";

        var sut = GetSut();
        var firstTube = await sut.GetSampleQcTubeKey(tubeBarcode);

        date = date.AddTicks(1);

        var secondTube = await sut.GetSampleQcTubeKey(tubeBarcode);

        firstTube.Barcode.Should().Be("123");
        firstTube.SampleType.Should().Be(SampleType.Sample);
        firstTube.Id.Should().Be("123");
        firstTube.IdSuffix.Should().Be("8DD6DD207ECD600");
        firstTube.TubeId.Should().Be("123_8DD6DD207ECD600");
        firstTube.UnreadableBarcode.Should().BeFalse();

        secondTube.Barcode.Should().Be("123");
        secondTube.SampleType.Should().Be(SampleType.Sample);
        secondTube.Id.Should().Be("123");
        secondTube.IdSuffix.Should().Be("8DD6DD207ECD601");
        secondTube.TubeId.Should().Be("123_8DD6DD207ECD601");
        secondTube.UnreadableBarcode.Should().BeFalse();
    }

    [Fact]
    public async Task GetSampleQCTubeKey_When_ComplexBarcode_AndUsingBarcodeMaskForTubeId_ShouldReturnUniqueTubeIds()
    {
        var settings = new SpecialSampleHandlingSettings
        {
            ExtractSampleIdFromBarcode = true,
            SampleIdStartPosition = 4,
            SampleIdLength = 4,
            ExtractSampleIdSuffixFromBarcode = true,
            SampleIdSuffixStartPosition = 11,
            SampleIdSuffixLength = 2,
            SampleTubesWithSameSampleId = true
        };
        _settingsMock.Setup(x => x.Value).Returns(settings);
        var date = new DateTime(2025, 03, 28, 8, 25, 0);
        _dateTimeMock.Setup(x => x.Now).Returns(() => new DateTimeOffset(date, TimeSpan.Zero));

        var sut = GetSut();
        var firstTube = await sut.GetSampleQcTubeKey("xxxS001xxx01");

        date = date.AddTicks(1);

        var secondTube = await sut.GetSampleQcTubeKey("xxxS001xxx02");

        firstTube.Barcode.Should().Be("xxxS001xxx01");
        firstTube.SampleType.Should().Be(SampleType.Sample);
        firstTube.Id.Should().Be("S001");
        firstTube.IdSuffix.Should().Be("01");
        firstTube.TubeId.Should().Be("S001_01");
        firstTube.UnreadableBarcode.Should().BeFalse();

        secondTube.Barcode.Should().Be("xxxS001xxx02");
        secondTube.SampleType.Should().Be(SampleType.Sample);
        secondTube.Id.Should().Be("S001");
        secondTube.IdSuffix.Should().Be("02");
        secondTube.TubeId.Should().Be("S001_02");
        secondTube.UnreadableBarcode.Should().BeFalse();
    }

    [Fact]
    public async Task GetSampleQCTubeKey_When_BarcodeMaskSuffixLengthExceedsBarcode_ShouldReturnTrimmedSuffix()
    {
        var settings = new SpecialSampleHandlingSettings
        {
            ExtractSampleIdFromBarcode = true,
            SampleIdStartPosition = 4,
            SampleIdLength = 4,
            ExtractSampleIdSuffixFromBarcode = true,
            SampleIdSuffixStartPosition = 11,
            SampleIdSuffixLength = 3,
            SampleTubesWithSameSampleId = true
        };
        _settingsMock.Setup(x => x.Value).Returns(settings);
        var date = new DateTime(2025, 03, 28, 8, 25, 0);
        _dateTimeMock.Setup(x => x.Now).Returns(() => new DateTimeOffset(date, TimeSpan.Zero));

        var sut = GetSut();
        var tube = await sut.GetSampleQcTubeKey("xxxS001xxx01");

        tube.Barcode.Should().Be("xxxS001xxx01");
        tube.SampleType.Should().Be(SampleType.Sample);
        tube.Id.Should().Be("S001");
        tube.IdSuffix.Should().Be("01");
        tube.TubeId.Should().Be("S001_01");
        tube.UnreadableBarcode.Should().BeFalse();
    }

    [Fact]
    public async Task GetSampleQCTubeKey_When_EmptyBarcode_ShouldReturnUnreadableBarcode()
    {
        var settings = new SpecialSampleHandlingSettings
        {
            ExtractSampleIdFromBarcode = false,
            ExtractSampleIdSuffixFromBarcode = false,
            SampleTubesWithSameSampleId = false
        };
        _settingsMock.Setup(x => x.Value).Returns(settings);
        var date = new DateTime(2025, 03, 28, 8, 25, 0);
        _dateTimeMock.Setup(x => x.Now).Returns(() => new DateTimeOffset(date, TimeSpan.Zero));

        var sut = GetSut();
        var tube = await sut.GetSampleQcTubeKey(string.Empty);

        tube.Barcode.Should().Be(string.Empty);
        tube.SampleType.Should().Be(SampleType.Sample);
        tube.Id.Should().Be(string.Empty);
        tube.IdSuffix.Should().Be("8DD6DD207ECD600");
        tube.TubeId.Should().Be("8DD6DD207ECD600");
        tube.UnreadableBarcode.Should().BeTrue();
    }

    [Fact]
    public async Task GetSampleQCTubeKey_When_UsingBarcodeMaskWithinRange_ShouldReturnMaskedSampleId()
    {
        var settings = new SpecialSampleHandlingSettings
        {
            ExtractSampleIdFromBarcode = true,
            SampleIdStartPosition = 1,
            SampleIdLength = 2
        };
        _settingsMock.Setup(x => x.Value).Returns(settings);

        var sut = GetSut();
        var result = await sut.GetSampleQcTubeKey("123");

        result.Id.Should().Be("12");
        result.SampleType.Should().Be(SampleType.Sample);
        result.UnreadableBarcode.Should().BeFalse();
    }

    [Fact]
    public async Task GetSampleQCTubeKey_When_BarcodeMaskOutOfRange_ShouldReturnUnreadableBarcode()
    {
        var settings = new SpecialSampleHandlingSettings
        {
            ExtractSampleIdFromBarcode = true,
            SampleIdStartPosition = 4,
            SampleIdLength = 2
        };
        _settingsMock.Setup(x => x.Value).Returns(settings);

        var sut = GetSut();
        var result = await sut.GetSampleQcTubeKey("123");

        result.Id.Should().Be(string.Empty);
        result.SampleType.Should().Be(SampleType.Sample);
        result.UnreadableBarcode.Should().BeTrue();
    }

    [Fact]
    public async Task GetSampleQCTubeKey_When_BarcodeMaskLengthExceedsBarcode_ShouldReturnTrimmedSampleId()
    {
        var settings = new SpecialSampleHandlingSettings
        {
            ExtractSampleIdFromBarcode = true,
            SampleIdStartPosition = 1,
            SampleIdLength = 5
        };
        _settingsMock.Setup(x => x.Value).Returns(settings);

        var sut = GetSut();
        var result = await sut.GetSampleQcTubeKey("123");

        result.Id.Should().Be("123");
        result.SampleType.Should().Be(SampleType.Sample);
        result.UnreadableBarcode.Should().BeFalse();
    }

    [Fact]
    public async Task GetSampleQCTubeKey_When_BarcodeMaskLengthIsZero_ShouldReturnUnreadableBarcode()
    {
        var settings = new SpecialSampleHandlingSettings
        {
            ExtractSampleIdFromBarcode = true,
            SampleIdStartPosition = 1,
            SampleIdLength = 0
        };
        _settingsMock.Setup(x => x.Value).Returns(settings);

        var sut = GetSut();
        var result = await sut.GetSampleQcTubeKey("123");

        result.Id.Should().Be(string.Empty);
        result.SampleType.Should().Be(SampleType.Sample);
        result.UnreadableBarcode.Should().BeTrue();
    }

    [Fact]
    public async Task GetSampleQCTubeKey_When_InvalidCombination_ShouldFallbackToTicksSuffix()
    {
        var settings = new SpecialSampleHandlingSettings
        {
            ExtractSampleIdFromBarcode = false,
            SampleIdStartPosition = 4,
            SampleIdLength = 4,
            ExtractSampleIdSuffixFromBarcode = true,
            SampleIdSuffixStartPosition = 11,
            SampleIdSuffixLength = 2,
            SampleTubesWithSameSampleId = true
        };
        _settingsMock.Setup(x => x.Value).Returns(settings);
        var date = new DateTime(2025, 03, 28, 8, 25, 0);
        _dateTimeMock.Setup(x => x.Now).Returns(() => new DateTimeOffset(date, TimeSpan.Zero));

        var sut = GetSut();
        var tube = await sut.GetSampleQcTubeKey("xxxS001xxx01");

        tube.Barcode.Should().Be("xxxS001xxx01");
        tube.SampleType.Should().Be(SampleType.Sample);
        tube.Id.Should().Be("xxxS001xxx01");
        tube.IdSuffix.Should().Be("8DD6DD207ECD600");
        tube.TubeId.Should().Be("xxxS001xxx01_8DD6DD207ECD600");
        tube.UnreadableBarcode.Should().BeFalse();
    }

    [Fact]
    public async Task GetSampleQCTubeKey_When_QCBarcode_ShouldReturnQualityControlSampleType()
    {
        _senderMock.Setup(x => x.Send(It.IsAny<IsQualityControlQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlResultDto(true, "QC-Name", true, "LotNumber", 1));

        var settings = new SpecialSampleHandlingSettings
        {
            ExtractSampleIdFromBarcode = false,
            ExtractSampleIdSuffixFromBarcode = false,
            SampleTubesWithSameSampleId = false
        };
        _settingsMock.Setup(x => x.Value).Returns(settings);
        var date = new DateTime(2025, 03, 28, 8, 25, 0);
        _dateTimeMock.Setup(x => x.Now).Returns(() => new DateTimeOffset(date, TimeSpan.Zero));

        var sut = GetSut();
        var result = await sut.GetSampleQcTubeKey("QC12345");

        result.Barcode.Should().Be("QC12345");
        result.SampleType.Should().Be(SampleType.QualityControl);
        result.Id.Should().Be("QC-Name");
        result.IdSuffix.Should().Be("8DD6DD207ECD600");
        result.TubeId.Should().Be("QC-Name_8DD6DD207ECD600");
        result.UnreadableBarcode.Should().BeFalse();
        result.QcLotNumber.Should().Be("LotNumber");
    }

    [Fact]
    public void GetSampleTubeKeyNoBarcode_When_SampleTubesWithoutSameSampleId_ShouldGenerateSuffix()
    {
        var settings = new SpecialSampleHandlingSettings
        {
            SampleTubesWithSameSampleId = false
        };
        _settingsMock.Setup(x => x.Value).Returns(settings);
        var date = new DateTime(2025, 03, 28, 8, 25, 0);
        _dateTimeMock.Setup(x => x.Now).Returns(new DateTimeOffset(date, TimeSpan.Zero));

        var sut = GetSut();
        var result = sut.GetSampleTubeKeyNoBarcode("Sample-1");

        result.Barcode.Should().Be(string.Empty);
        result.SampleType.Should().Be(SampleType.Sample);
        result.Id.Should().Be("Sample-1");
        result.IdSuffix.Should().Be("8DD6DD207ECD600");
        result.TubeId.Should().Be("Sample-1_8DD6DD207ECD600");
        result.UnreadableBarcode.Should().BeFalse();
    }

    [Fact]
    public void GetSampleTubeKeyNoBarcode_When_SampleTubesWithSameSampleId_ShouldNotAppendSuffix()
    {
        var settings = new SpecialSampleHandlingSettings
        {
            SampleTubesWithSameSampleId = true
        };
        _settingsMock.Setup(x => x.Value).Returns(settings);

        var sut = GetSut();
        var result = sut.GetSampleTubeKeyNoBarcode("Sample-1");

        result.Barcode.Should().Be(string.Empty);
        result.SampleType.Should().Be(SampleType.Sample);
        result.Id.Should().Be("Sample-1");
        result.IdSuffix.Should().Be(string.Empty);
        result.TubeId.Should().Be("Sample-1");
        result.UnreadableBarcode.Should().BeFalse();
    }
}