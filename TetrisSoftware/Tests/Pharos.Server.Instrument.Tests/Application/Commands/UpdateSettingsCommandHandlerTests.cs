using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Instrument.Application.Commands.UpdateSettings;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument;
using InstrumentSettings = Pharos.Server.Application.Settings.Instrument.InstrumentSettings;

namespace Pharos.Server.Instrument.Application.Commands;

public class UpdateSettingsCommandHandlerTests
{
    private const string _instrumentNumber = "SEED_001";

    [Fact]
    [Trait("ProductRequirement", "PR_2803")]
    public async Task Handle_WithDefaultBarcodeSettings_ShouldMapDefaultSymbologiesToInternalAndExternalLists()
    {
        // Act
        var result = await UpdateSettingsAsync(new TubeBarcodesSettings());

        // Assert
        var expected = new[]
        {
            EnumBarcodeSymbology.BarcodeSymbologyCode39CheckSumOn,
            EnumBarcodeSymbology.BarcodeSymbologyCode93,
            EnumBarcodeSymbology.BarcodeSymbologyCode128,
            EnumBarcodeSymbology.BarcodeSymbologyInterleaved2Of5ChecksumOn
        };

        result.SampleBarcodeUsage.InternalSymbologies.Should().Equal(expected);
        result.SampleBarcodeUsage.ExternalSymbologies.Should().Equal(expected);
        result.SampleBarcodeUsage.SampleBarcodePrefixes.Should().BeEmpty();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2803")]
    public async Task Handle_WithChecksumsDisabled_ShouldMapChecksumOffSymbologies()
    {
        // Arrange
        var settings = CreateSettingsWithOnlyConfigurableChecksumSymbologiesEnabled();
        SetRequiredSetting(settings, "Code39Checksum", false);
        SetRequiredSetting(settings, "Interleaved2Of5Checksum", false);

        // Act
        var result = await UpdateSettingsAsync(settings);

        // Assert
        var expected = new[]
        {
            EnumBarcodeSymbology.BarcodeSymbologyCode39ChecksumOff,
            EnumBarcodeSymbology.BarcodeSymbologyInterleaved2Of5CheckSumOff
        };

        result.SampleBarcodeUsage.InternalSymbologies.Should().Equal(expected);
        result.SampleBarcodeUsage.ExternalSymbologies.Should().Equal(expected);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2803")]
    public async Task Handle_WithBarcodeTypesDisabled_ShouldOmitSymbologiesRegardlessOfStoredChecksum()
    {
        // Arrange
        var settings = CreateSettingsWithOnlyConfigurableChecksumSymbologiesEnabled();
        settings.Code39 = false;
        settings.Interleaved2Of5 = false;
        SetRequiredSetting(settings, "Code39Checksum", true);
        SetRequiredSetting(settings, "Interleaved2Of5Checksum", true);

        // Act
        var result = await UpdateSettingsAsync(settings);

        // Assert
        result.SampleBarcodeUsage.InternalSymbologies.Should().BeEmpty();
        result.SampleBarcodeUsage.ExternalSymbologies.Should().BeEmpty();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2803")]
    public async Task Handle_WithConfiguredBarcodeSettings_ShouldPreserveBarcodeOrderAndPrefixes()
    {
        // Arrange
        var settings = new TubeBarcodesSettings
        {
            Code39 = false,
            Code93 = false,
            Code128 = true,
            Interleaved2Of5 = false,
            CodaBar = true,
            Ean8 = true,
            Industrial2Of5 = true,
            Coop2Of5 = true,
            QrCode = true,
            DataMatrix = true,
            Prefix1 = "A",
            Prefix2 = "B",
            Prefix3 = "C",
            Prefix4 = "D",
            Prefix5 = "E"
        };

        // Act
        var result = await UpdateSettingsAsync(settings);

        // Assert
        var expected = new[]
        {
            EnumBarcodeSymbology.BarcodeSymbologyCode128,
            EnumBarcodeSymbology.BarcodeSymbologyCodaBar,
            EnumBarcodeSymbology.BarcodeSymbologyEan8,
            EnumBarcodeSymbology.BarcodeSymbologyIndustrial2Of5,
            EnumBarcodeSymbology.BarcodeSymbologyCoop2Of5,
            EnumBarcodeSymbology.BarcodeSymbologyQrcode,
            EnumBarcodeSymbology.BarcodeSymbologyDataMatrix
        };

        result.SampleBarcodeUsage.InternalSymbologies.Should().Equal(expected);
        result.SampleBarcodeUsage.ExternalSymbologies.Should().Equal(expected);
        result.SampleBarcodeUsage.SampleBarcodePrefixes.Should().Equal("A", "B", "C", "D", "E");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2803")]
    public async Task Handle_WithEmptyAndSpacePaddedPrefixes_ShouldSendOnlyTrimmedNonEmptyPrefixes()
    {
        // Arrange
        var settings = new TubeBarcodesSettings
        {
            Prefix1 = " SMP ",
            Prefix2 = "",
            Prefix3 = "QC",
            Prefix4 = "   ",
            Prefix5 = " LOT "
        };

        // Act
        var result = await UpdateSettingsAsync(settings);

        // Assert
        result.SampleBarcodeUsage.SampleBarcodePrefixes.Should().Equal("SMP", "QC", "LOT");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2803")]
    public async Task Handle_ShouldSendSampleBarcodeUsageInUpdateSettings()
    {
        // Arrange
        var sentSettings = await UpdateSettingsAsync(new TubeBarcodesSettings
        {
            Code39 = true,
            Code93 = false,
            Code128 = false,
            Interleaved2Of5 = false,
            CodaBar = true,
            Prefix1 = "P1"
        });

        // Assert
        sentSettings.SampleBarcodeUsage.Should().NotBeNull();
        sentSettings.SampleBarcodeUsage.InternalSymbologies.Should().Equal(
            EnumBarcodeSymbology.BarcodeSymbologyCode39CheckSumOn,
            EnumBarcodeSymbology.BarcodeSymbologyCodaBar);
        sentSettings.SampleBarcodeUsage.ExternalSymbologies.Should().Equal(
            EnumBarcodeSymbology.BarcodeSymbologyCode39CheckSumOn,
            EnumBarcodeSymbology.BarcodeSymbologyCodaBar);
        sentSettings.SampleBarcodeUsage.SampleBarcodePrefixes.Should().Equal("P1");
    }

    private static IOptionsSnapshot<T> CreateOptions<T>(T value) where T : class
    {
        var mock = new Mock<IOptionsSnapshot<T>>();
        mock.Setup(x => x.Value).Returns(value);
        return mock.Object;
    }

    private static TubeBarcodesSettings CreateSettingsWithOnlyConfigurableChecksumSymbologiesEnabled() =>
        new()
        {
            Code39 = true,
            Code93 = false,
            Code128 = false,
            Interleaved2Of5 = true,
            CodaBar = false,
            Ean8 = false,
            Industrial2Of5 = false,
            Coop2Of5 = false,
            QrCode = false,
            DataMatrix = false
        };

    private static void SetRequiredSetting(TubeBarcodesSettings settings, string propertyName, bool value)
    {
        var property = typeof(TubeBarcodesSettings).GetProperty(propertyName);
        property.Should().NotBeNull($"{propertyName} is required by PBI 39588");
        property!.SetValue(settings, value);
    }

    private static async Task<Settings> UpdateSettingsAsync(TubeBarcodesSettings tubeBarcodeSettings)
    {
        Settings? sentSettings = null;
        var connectionMock = new Mock<IInstrumentConnection>();
        connectionMock
            .Setup(x => x.UpdateSettingsAsync(It.IsAny<Settings>(), It.IsAny<CancellationToken>()))
            .Callback<Settings, CancellationToken>((settings, _) => sentSettings = settings)
            .Returns(Task.CompletedTask);

        var session = new CommunicationSession
        {
            SessionId = Guid.NewGuid(),
            InstrumentNumber = _instrumentNumber,
            Connection = connectionMock.Object
        };

        var sessionManagerMock = new Mock<ICommunicationSessionManager>();
        sessionManagerMock.Setup(x => x.GetSession(_instrumentNumber)).Returns(session);

        var sut = new UpdateSettingsCommandHandler(
            sessionManagerMock.Object,
            CreateOptions(new InstrumentSettings()),
            CreateOptions(new TubeTypesSettings()),
            CreateOptions(tubeBarcodeSettings),
            CreatePublisher());

        await sut.Handle(new UpdateSettingsCommand(_instrumentNumber), CancellationToken.None);

        return sentSettings!;
    }

    private static IPublisher CreatePublisher()
    {
        var publisherMock = new Mock<IPublisher>();
        publisherMock.Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return publisherMock.Object;
    }
}
