using MediatR;
using Microsoft.Extensions.Options;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Queries;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Queries;

public class IdentifyTrayOrSampleBarcodeQueryHandlerTests
{
    private const string _barcode = "SAMPLE123";
    private const string _sampleId = "S123";
    private const int _requestId = 456;

    private readonly Mock<ISampleDbContext> _dbContextMock = new();
    private readonly Mock<ISampleQcIdProvider> _sampleIdProviderMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<IInstrumentSampleStorage> _sampleStorageMock = new();
    private readonly Mock<IInstrumentManager> _instrumentManagerMock = new();
    private readonly Mock<IInstrument> _instrumentMock = new();
    private readonly Mock<IOptionsSnapshot<TubeBarcodesSettings>> _tubeBarcodesSettingsMock = new();

    public IdentifyTrayOrSampleBarcodeQueryHandlerTests()
    {
        SetPrefixes();
    }

    [Fact]
    public async Task Handle_WhenTrayExistsButIsNotLoaded_ShouldReturnTrayOutsideClosedDrawer()
    {
        // Arrange
        const string barcode = "TRAY001";
        SetupSampleTrays(new SampleTray { Id = barcode });

        // Act
        var result = await HandleAsync(barcode);

        // Assert
        result.Type.Should().Be(BarcodeType.Tray);
        result.IsTrayInClosedDrawer.Should().BeFalse();
        _sampleIdProviderMock.Verify(x => x.GetSampleQcTubeKey(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTrayExistsInClosedDrawer_ShouldReturnTrayInClosedDrawer()
    {
        // Arrange
        const string barcode = "TRAY001";
        SetupLoadedTray(barcode, LockState.ClosedLocked);

        // Act
        var result = await HandleAsync(barcode);

        // Assert
        result.Type.Should().Be(BarcodeType.Tray);
        result.IsTrayInClosedDrawer.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTrayExistsInOpenDrawer_ShouldReturnTrayOutsideClosedDrawer()
    {
        // Arrange
        const string barcode = "TRAY001";
        SetupLoadedTray(barcode, LockState.Open);

        // Act
        var result = await HandleAsync(barcode);

        // Assert
        result.Type.Should().Be(BarcodeType.Tray);
        result.IsTrayInClosedDrawer.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenNoPrefixesAndRequestExists_ShouldReturnExistingSample()
    {
        // Arrange
        SetupSampleTrays();
        SetupSampleKey(_sampleId);
        SetupRequest(_sampleId, requestExists: true, requestId: _requestId);

        // Act
        var result = await HandleAsync(_barcode);

        // Assert
        result.Type.Should().Be(BarcodeType.Sample);
        result.RequestId.Should().Be(_requestId);
    }

    [Fact]
    public async Task Handle_WhenConfiguredPrefixMatchesAndRequestExists_ShouldReturnExistingSample()
    {
        // Arrange
        SetupSampleTrays();
        SetPrefixes("OTHER", "   ", "SAMPLE");
        SetupSampleKey(_sampleId);
        SetupRequest(_sampleId, requestExists: true, requestId: _requestId);

        // Act
        var result = await HandleAsync(_barcode);

        // Assert
        result.Type.Should().Be(BarcodeType.Sample);
        result.RequestId.Should().Be(_requestId);
    }

    [Theory]
    [InlineData("OTHER")]
    [InlineData("sample")]
    public async Task Handle_WhenConfiguredPrefixDoesNotMatchExactly_ShouldReturnUnknownWithoutLookingUpSample(string prefix)
    {
        // Arrange
        SetupSampleTrays();
        SetPrefixes(prefix);

        // Act
        var result = await HandleAsync(_barcode);

        // Assert
        result.Type.Should().Be(BarcodeType.UnknownBarcode);
        result.CanQueryMissingRequest.Should().BeFalse();
        _sampleIdProviderMock.Verify(x => x.GetSampleQcTubeKey(It.IsAny<string>()), Times.Never);
        _senderMock.Verify(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOnlyWhitespacePrefixIsConfigured_ShouldPreserveLegacyHandling()
    {
        // Arrange
        SetupSampleTrays();
        SetPrefixes("   ");
        SetupSampleKey(_sampleId);
        SetupRequest(_sampleId, requestExists: true, requestId: _requestId);

        // Act
        var result = await HandleAsync(_barcode);

        // Assert
        result.Type.Should().Be(BarcodeType.Sample);
    }

    [Theory]
    [InlineData(true, "S123")]
    [InlineData(false, "")]
    public async Task Handle_WhenSampleKeyCannotIdentifySample_ShouldReturnUnknownWithoutLookingUpRequest(
        bool unreadableBarcode,
        string sampleId)
    {
        // Arrange
        SetupSampleTrays();
        SetupSampleKey(sampleId, unreadableBarcode);

        // Act
        var result = await HandleAsync(_barcode);

        // Assert
        result.Type.Should().Be(BarcodeType.UnknownBarcode);
        _senderMock.Verify(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSampleRequestDoesNotExist_ShouldReturnUnknownBarcode()
    {
        // Arrange
        SetupSampleTrays();
        SetPrefixes("SAMPLE");
        SetupSampleKey(_sampleId);
        SetupRequest(_sampleId, requestExists: false);

        // Act
        var result = await HandleAsync(_barcode);

        // Assert
        result.Type.Should().Be(BarcodeType.UnknownBarcode);
        result.CanQueryMissingRequest.Should().BeTrue();
    }

    private Task<SampleBarcodeResult> HandleAsync(string barcode)
    {
        return CreateHandler().Handle(
            new IdentifyTrayOrSampleBarcodeQuery(barcode),
            TestContext.Current.CancellationToken);
    }

    private IdentifyTrayOrSampleBarcodeQueryHandler CreateHandler()
    {
        return new IdentifyTrayOrSampleBarcodeQueryHandler(
            _dbContextMock.Object,
            _sampleIdProviderMock.Object,
            _senderMock.Object,
            _sampleStorageMock.Object,
            _instrumentManagerMock.Object,
            _tubeBarcodesSettingsMock.Object);
    }

    private void SetupSampleTrays(params SampleTray[] trays)
    {
        _dbContextMock.Setup(x => x.SampleTrays).ReturnsDbSet(trays);
    }

    private void SetupLoadedTray(string barcode, LockState lockState)
    {
        const string instrumentNumber = "INST001";
        var tray = new SampleTray
        {
            Id = barcode,
            InstrumentNumber = instrumentNumber,
            Drawer = DrawerType.Sample1,
            TrayPosition = 1
        };

        SetupSampleTrays(tray);
        _sampleStorageMock.Setup(x => x.GetTray(barcode)).Returns(tray);
        _instrumentManagerMock.Setup(x => x.GetInstrument(instrumentNumber)).Returns(_instrumentMock.Object);
        _instrumentMock.Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, lockState, false));
    }

    private void SetupSampleKey(string sampleId, bool unreadableBarcode = false)
    {
        _sampleIdProviderMock
            .Setup(x => x.GetSampleQcTubeKey(_barcode))
            .ReturnsAsync(new SampleQcTubeKey(
                _barcode,
                SampleType.Sample,
                sampleId,
                string.Empty,
                unreadableBarcode,
                true,
                string.Empty,
                0));
    }

    private void SetupRequest(string sampleId, bool requestExists, int requestId = 0)
    {
        _senderMock
            .Setup(x => x.Send(
                It.Is<GetRequestIdQuery>(query => query.SampleId == sampleId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(requestId, DateTimeOffset.Now, requestExists));
    }

    private void SetPrefixes(params string[] prefixes)
    {
        _tubeBarcodesSettingsMock
            .SetupGet(x => x.Value)
            .Returns(new TubeBarcodesSettings
            {
                Prefix1 = prefixes.ElementAtOrDefault(0) ?? string.Empty,
                Prefix2 = prefixes.ElementAtOrDefault(1) ?? string.Empty,
                Prefix3 = prefixes.ElementAtOrDefault(2) ?? string.Empty,
                Prefix4 = prefixes.ElementAtOrDefault(3) ?? string.Empty,
                Prefix5 = prefixes.ElementAtOrDefault(4) ?? string.Empty
            });
    }
}