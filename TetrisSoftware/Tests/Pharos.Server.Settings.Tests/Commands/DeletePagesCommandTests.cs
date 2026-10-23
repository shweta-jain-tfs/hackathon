using MediatR;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Application.AuditTrail;
using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Request;
using Pharos.Server.Settings.Application.Commands.DeleteSettings;
using Pharos.Server.Settings.Application.Commands.NotifySettingsChanged;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Settings.Commands;

public class DeletePagesCommandTests
{
    private readonly Mock<ISettingsDbContext> _mockDbContext;
    private DeletePagesCommand? _request;
    private readonly DeletePagesCommandHandler _sut;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<DeletePagesCommandHandler>> _mockLogger = new();
    private readonly Mock<ISender> _mockSender = new();
    private readonly Mock<IConfiguration> _mockConfiguration = new();
    private readonly Mock<ISettingsPagesLoader> _mockPagesLoader = new();
    private readonly IPageTreeNavigator _pageTreeNavigator = new PageTreeNavigator();

    public DeletePagesCommandTests()
    {
        _mockDbContext = new Mock<ISettingsDbContext>();
        _mockPagesLoader
            .Setup(loader => loader.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockSender
            .Setup(s => s.Send(It.IsAny<IsTubeTypeUsedByAnyRequestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockSender
            .Setup(s => s.Send(It.IsAny<IsTubeTypeUsedByAnyQualityControlQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sut = CreateSut(_mockDbContext);
    }

    private DeletePagesCommandHandler CreateSut(
        Mock<ISettingsDbContext> dbContext,
        Mock<IConfiguration>? configuration = null,
        Mock<ISettingsPagesLoader>? pagesLoader = null)
    {
        return new DeletePagesCommandHandler(
            dbContext.Object,
            _mockLogger.Object,
            _mockSender.Object,
            (configuration ?? _mockConfiguration).Object,
            (pagesLoader ?? _mockPagesLoader).Object,
            _pageTreeNavigator);
    }

    private static Mock<ISettingsPagesLoader> CreatePagesLoaderWithMinItems(
        string rootPageKey,
        string pageListKey,
        int minItems)
    {
        var pagesLoader = new Mock<ISettingsPagesLoader>();
        pagesLoader
            .Setup(loader => loader.LoadAsync(rootPageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Page>
            {
                [rootPageKey] = new()
                {
                    Key = rootPageKey,
                    Children =
                    [
                        new PageList
                        {
                            Key = pageListKey,
                            MinItems = minItems,
                            PageStructure = new Page { Key = pageListKey },
                            PageEntries = []
                        }
                    ]
                }
            });

        return pagesLoader;
    }

    [Fact]
    public async Task ShouldCallAuditTrailEventWhenPagesAreDeleted()
    {
        var pageKey = "LIS:LisChannels:0";
        var settings = new List<Setting>
        {
            new() { Key = $"{pageKey}:Name", Value = "Test Channel" },
            new() { Key = $"{pageKey}:IsEnabled", Value = "true" }
        };

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);
        _request = new DeletePagesCommand(new[] { pageKey });

        await _sut.Handle(_request, TestContext.Current.CancellationToken);

        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Exactly(2));
        _mockSender.Verify(sender =>
            sender.Send(It.Is<CreateAuditTrailRecordCommand>(cmd =>
                cmd.EventId == EventId.SettingsChanged &&
                cmd.Level == EventLevel.Information &&
                cmd.EventData.Any(data =>
                    data.Data.ContainsKey(EventDataFields.ModifiedSettings.ToString())
                )
            ), It.IsAny<CancellationToken>()), Times.Once);
        _mockSender.Verify(sender =>
            sender.Send(It.Is<NotifySettingsChangedCommand>(cmd =>
                    cmd.DeletedKeys != null &&
                    cmd.DeletedKeys.SequenceEqual(new[] { pageKey })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeletePages_WithSingleLisChannel_ShouldRemoveAllRelatedSettingsFromDb()
    {
        var lisChannelKey = "LIS:LisChannels:0";
        var settings = new List<Setting>
        {
            new() { Key = $"{lisChannelKey}:Name", Value = "Test LIS Channel" },
            new() { Key = $"{lisChannelKey}:IsEnabled", Value = "true" },
            new() { Key = $"{lisChannelKey}:ProtocolType", Value = "1" },
            new() { Key = $"{lisChannelKey}:ImportExportSettings:ImportEnabled", Value = "true" },
            new() { Key = $"{lisChannelKey}:AstmSettings:CommunicationSettings:CommunicationMode", Value = "1" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);
        var mockConfiguration = new Mock<IConfiguration>();
        var sut = CreateSut(mockDbContext, mockConfiguration);

        var command = new DeletePagesCommand(new[] { lisChannelKey });

        await sut.Handle(command, TestContext.Current.CancellationToken);

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Exactly(5));
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePages_WithMultipleLisChannels_ShouldRemoveAllRelatedSettingsFromDb()
    {
        var lisChannelKey1 = "LIS:LisChannels:0";
        var lisChannelKey2 = "LIS:LisChannels:1";
        var settings = new List<Setting>
        {
            new() { Key = $"{lisChannelKey1}:Name", Value = "LIS Channel 1" },
            new() { Key = $"{lisChannelKey1}:IsEnabled", Value = "true" },
            new() { Key = $"{lisChannelKey1}:ProtocolType", Value = "1" },
            new() { Key = $"{lisChannelKey2}:Name", Value = "LIS Channel 2" },
            new() { Key = $"{lisChannelKey2}:IsEnabled", Value = "false" },
            new() { Key = $"{lisChannelKey2}:ProtocolType", Value = "2" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);
        var mockConfiguration = new Mock<IConfiguration>();
        var sut = CreateSut(mockDbContext, mockConfiguration);

        var command = new DeletePagesCommand(new[] { lisChannelKey1, lisChannelKey2 });

        await sut.Handle(command, TestContext.Current.CancellationToken);

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Exactly(6));
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePages_WhenDeletingLastItemWouldGoBelowMinItems_ShouldSkipDeletion()
    {
        var lisChannelKey = "Lis:LisChannels:0";
        var settings = new List<Setting>
        {
            new() { Key = $"{lisChannelKey}:Name", Value = "LIS Channel 1" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var pagesLoader = CreatePagesLoaderWithMinItems("Lis", "Lis:LisChannels", 1);
        var sut = CreateSut(mockDbContext, pagesLoader: pagesLoader);

        var response = await sut.Handle(new DeletePagesCommand([lisChannelKey]), TestContext.Current.CancellationToken);

        response.Deleted.Should().Be(0);
        response.Skipped.Should().Be(1);
        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockSender.Verify(s => s.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeletePages_WhenBulkDeleteWouldGoBelowMinItems_ShouldSkipSelectedItems()
    {
        var lisChannelKey1 = "Lis:LisChannels:0";
        var lisChannelKey2 = "Lis:LisChannels:1";
        var lisChannelKey3 = "Lis:LisChannels:2";
        var settings = new List<Setting>
        {
            new() { Key = $"{lisChannelKey1}:Name", Value = "LIS Channel 1" },
            new() { Key = $"{lisChannelKey2}:Name", Value = "LIS Channel 2" },
            new() { Key = $"{lisChannelKey3}:Name", Value = "LIS Channel 3" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var pagesLoader = CreatePagesLoaderWithMinItems("Lis", "Lis:LisChannels", 1);
        var sut = CreateSut(mockDbContext, pagesLoader: pagesLoader);

        var response = await sut.Handle(
            new DeletePagesCommand([lisChannelKey1, lisChannelKey2, lisChannelKey3]),
            TestContext.Current.CancellationToken);

        response.Deleted.Should().Be(0);
        response.Skipped.Should().Be(3);
        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockSender.Verify(s => s.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_315")]
    [Trait("ModuleRequirement", "MR_5478")]
    [Fact]
    public async Task DeletePages_WithNormalDefaultTubeType_ShouldSkipDeletion()
    {
        var tubeEntryKey = "Samples:TubeTypes:TubeTypeList:0";
        var tubeDefinitionId = "1";

        var settings = new List<Setting>
        {
            // Defaults now come from DB:
            new() { Key = "Samples:TubeTypes:NormalTubeDefinitionId", Value = tubeDefinitionId },
            new() { Key = "Samples:TubeTypes:PediatricTubeDefinitionId", Value = "2" },

            new() { Key = $"{tubeEntryKey}:TubeDefinitionId", Value = tubeDefinitionId },
            new() { Key = $"{tubeEntryKey}:Name", Value = "Normal tube" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["Samples:TubeTypes:NormalTubeDefinitionId"]).Returns(tubeDefinitionId);
        mockConfiguration.Setup(c => c["Samples:TubeTypes:PediatricTubeDefinitionId"]).Returns("2");
        var sut = CreateSut(mockDbContext, mockConfiguration);

        var command = new DeletePagesCommand(new[] { tubeEntryKey });

        var response = await sut.Handle(command, TestContext.Current.CancellationToken);

        response.Deleted.Should().Be(0);
        response.Skipped.Should().Be(1);
        response.SkippedIds.Should().ContainSingle().Which.Should().Be(1);
        response.SkippedPageListEntryKeys.Should().ContainSingle().Which.Should().Be(tubeEntryKey);
        response.SkippedTubeTypeReasons.Should().ContainSingle();
        response.SkippedTubeTypeReasons[0].IsDefaultTubeType.Should().BeTrue();

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockSender.Verify(s => s.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_315")]
    [Trait("ModuleRequirement", "MR_5478")]
    [Fact]
    public async Task DeletePages_WithPediatricDefaultTubeType_ShouldSkipDeletion()
    {
        var tubeEntryKey = "Samples:TubeTypes:TubeTypeList:0";
        var tubeDefinitionId = "2";

        var settings = new List<Setting>
        {
            // Defaults now come from DB:
            new() { Key = "Samples:TubeTypes:NormalTubeDefinitionId", Value = "1" },
            new() { Key = "Samples:TubeTypes:PediatricTubeDefinitionId", Value = tubeDefinitionId },

            new() { Key = $"{tubeEntryKey}:TubeDefinitionId", Value = tubeDefinitionId },
            new() { Key = $"{tubeEntryKey}:Name", Value = "Pediatric tube" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["Samples:TubeTypes:NormalTubeDefinitionId"]).Returns("1");
        mockConfiguration.Setup(c => c["Samples:TubeTypes:PediatricTubeDefinitionId"]).Returns(tubeDefinitionId);
        var sut = CreateSut(mockDbContext, mockConfiguration);

        var command = new DeletePagesCommand(new[] { tubeEntryKey });

        var response = await sut.Handle(command, TestContext.Current.CancellationToken);

        response.Deleted.Should().Be(0);
        response.Skipped.Should().Be(1);
        response.SkippedIds.Should().ContainSingle().Which.Should().Be(2);
        response.SkippedPageListEntryKeys.Should().ContainSingle().Which.Should().Be(tubeEntryKey);
        response.SkippedTubeTypeReasons.Should().ContainSingle();
        response.SkippedTubeTypeReasons[0].IsDefaultTubeType.Should().BeTrue();

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockSender.Verify(s => s.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_315")]
    [Trait("ModuleRequirement", "MR_5478")]
    [Fact]
    public async Task DeletePages_WithBothDefaultTubeTypesSelected_ShouldSkipBoth()
    {
        var normalEntryKey = "Samples:TubeTypes:TubeTypeList:0";
        var pediatricEntryKey = "Samples:TubeTypes:TubeTypeList:1";

        var normalTubeDefinitionId = "1";
        var pediatricTubeDefinitionId = "2";

        var settings = new List<Setting>
        {
            // Defaults now come from DB:
            new() { Key = "Samples:TubeTypes:NormalTubeDefinitionId", Value = normalTubeDefinitionId },
            new() { Key = "Samples:TubeTypes:PediatricTubeDefinitionId", Value = pediatricTubeDefinitionId },

            new() { Key = $"{normalEntryKey}:TubeDefinitionId", Value = normalTubeDefinitionId },
            new() { Key = $"{normalEntryKey}:Name", Value = "Normal tube" },

            new() { Key = $"{pediatricEntryKey}:TubeDefinitionId", Value = pediatricTubeDefinitionId },
            new() { Key = $"{pediatricEntryKey}:Name", Value = "Pediatric tube" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["Samples:TubeTypes:NormalTubeDefinitionId"]).Returns(normalTubeDefinitionId);
        mockConfiguration.Setup(c => c["Samples:TubeTypes:PediatricTubeDefinitionId"]).Returns(pediatricTubeDefinitionId);
        var sut = CreateSut(mockDbContext, mockConfiguration);

        var command = new DeletePagesCommand(new[] { normalEntryKey, pediatricEntryKey });

        var response = await sut.Handle(command, TestContext.Current.CancellationToken);

        response.Deleted.Should().Be(0);
        response.Skipped.Should().Be(2);
        response.SkippedIds.Should().BeEquivalentTo([1, 2]);
        response.SkippedPageListEntryKeys.Should().BeEquivalentTo([normalEntryKey, pediatricEntryKey]);
        response.SkippedTubeTypeReasons.Should().HaveCount(2);
        response.SkippedTubeTypeReasons.Should().OnlyContain(reason => reason.IsDefaultTubeType);

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockSender.Verify(s => s.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_315")]
    [Trait("ModuleRequirement", "MR_5478")]
    [Fact]
    public async Task DeletePages_WithTubeType_WhenDefaultIdsCannotBeResolved_ShouldSkipDeletion()
    {
        var tubeEntryKey = "Samples:TubeTypes:TubeTypeList:0";
        var tubeDefinitionId = "1";

        var settings = new List<Setting>
        {
            // Defaults exist but are not parseable -> should skip
            new() { Key = "Samples:TubeTypes:NormalTubeDefinitionId", Value = "not-an-int" },
            new() { Key = "Samples:TubeTypes:PediatricTubeDefinitionId", Value = "not-an-int" },

            new() { Key = $"{tubeEntryKey}:TubeDefinitionId", Value = tubeDefinitionId },
            new() { Key = $"{tubeEntryKey}:Name", Value = "Tube" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var mockConfiguration = new Mock<IConfiguration>();
        var sut = CreateSut(mockDbContext, mockConfiguration);

        var command = new DeletePagesCommand(new[] { tubeEntryKey });

        var response = await sut.Handle(command, TestContext.Current.CancellationToken);

        response.Deleted.Should().Be(0);
        response.Skipped.Should().Be(1);
        response.SkippedPageListEntryKeys.Should().ContainSingle().Which.Should().Be(tubeEntryKey);

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_315")]
    [Trait("ModuleRequirement", "MR_5478")]
    [Fact]
    public async Task DeletePages_WithTubeType_WhenTubeDefinitionIdIsMissing_ShouldSkipDeletion()
    {
        var tubeEntryKey = "Samples:TubeTypes:TubeTypeList:0";

        var settings = new List<Setting>
        {
            // Defaults now come from DB:
            new() { Key = "Samples:TubeTypes:NormalTubeDefinitionId", Value = "1" },
            new() { Key = "Samples:TubeTypes:PediatricTubeDefinitionId", Value = "2" },

            // No TubeDefinitionId setting stored for this entry
            new() { Key = $"{tubeEntryKey}:Name", Value = "Tube" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var mockConfiguration = new Mock<IConfiguration>();
        var sut = CreateSut(mockDbContext, mockConfiguration);

        var command = new DeletePagesCommand(new[] { tubeEntryKey });

        var response = await sut.Handle(command, TestContext.Current.CancellationToken);

        response.Deleted.Should().Be(0);
        response.Skipped.Should().Be(1);
        response.SkippedPageListEntryKeys.Should().ContainSingle().Which.Should().Be(tubeEntryKey);

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_315")]
    [Trait("ModuleRequirement", "MR_5478")]
    [Trait("ProductRequirement", "PR_316")]
    [Trait("ModuleRequirement", "MR_6034")]
    [Fact]
    public async Task DeletePages_WithTubeTypeUsedByAnyRequest_ShouldSkipDeletion()
    {
        var tubeEntryKey = "Samples:TubeTypes:TubeTypeList:0";
        var tubeDefinitionId = "3";

        var settings = new List<Setting>
        {
            new() { Key = "Samples:TubeTypes:NormalTubeDefinitionId", Value = "1" },
            new() { Key = "Samples:TubeTypes:PediatricTubeDefinitionId", Value = "2" },
            new() { Key = $"{tubeEntryKey}:TubeDefinitionId", Value = tubeDefinitionId },
            new() { Key = $"{tubeEntryKey}:Name", Value = "Tube" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["Samples:TubeTypes:NormalTubeDefinitionId"]).Returns("1");
        mockConfiguration.Setup(c => c["Samples:TubeTypes:PediatricTubeDefinitionId"]).Returns("2");

        _mockSender
            .Setup(s => s.Send(It.IsAny<IsTubeTypeUsedByAnyRequestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut(mockDbContext, mockConfiguration);

        var response = await sut.Handle(new DeletePagesCommand([tubeEntryKey]), TestContext.Current.CancellationToken);

        response.Deleted.Should().Be(0);
        response.Skipped.Should().Be(1);
        response.SkippedIds.Should().BeEmpty();
        response.SkippedPageListEntryKeys.Should().ContainSingle().Which.Should().Be(tubeEntryKey);
        response.SkippedTubeTypeReasons.Should().ContainSingle();
        response.SkippedTubeTypeReasons[0].PageListEntryKey.Should().Be(tubeEntryKey);
        response.SkippedTubeTypeReasons[0].UsedByRequests.Should().BeTrue();
        response.SkippedTubeTypeReasons[0].UsedByQualityControls.Should().BeFalse();

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockSender.Verify(s => s.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_315")]
    [Trait("ModuleRequirement", "MR_5478")]
    [Trait("ProductRequirement", "PR_316")]
    [Trait("ModuleRequirement", "MR_6034")]
    [Fact]
    public async Task DeletePages_WithTubeTypeUsedByAnyQualityControl_ShouldSkipDeletion()
    {
        var tubeEntryKey = "Samples:TubeTypes:TubeTypeList:0";
        var tubeDefinitionId = "3";

        var settings = new List<Setting>
        {
            new() { Key = "Samples:TubeTypes:NormalTubeDefinitionId", Value = "1" },
            new() { Key = "Samples:TubeTypes:PediatricTubeDefinitionId", Value = "2" },
            new() { Key = $"{tubeEntryKey}:TubeDefinitionId", Value = tubeDefinitionId },
            new() { Key = $"{tubeEntryKey}:Name", Value = "Tube" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["Samples:TubeTypes:NormalTubeDefinitionId"]).Returns("1");
        mockConfiguration.Setup(c => c["Samples:TubeTypes:PediatricTubeDefinitionId"]).Returns("2");

        _mockSender
            .Setup(s => s.Send(It.IsAny<IsTubeTypeUsedByAnyRequestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockSender
            .Setup(s => s.Send(It.IsAny<IsTubeTypeUsedByAnyQualityControlQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut(mockDbContext, mockConfiguration);

        var response = await sut.Handle(new DeletePagesCommand([tubeEntryKey]), TestContext.Current.CancellationToken);

        response.Deleted.Should().Be(0);
        response.Skipped.Should().Be(1);
        response.SkippedIds.Should().BeEmpty();
        response.SkippedPageListEntryKeys.Should().ContainSingle().Which.Should().Be(tubeEntryKey);
        response.SkippedTubeTypeReasons.Should().ContainSingle();
        response.SkippedTubeTypeReasons[0].PageListEntryKey.Should().Be(tubeEntryKey);
        response.SkippedTubeTypeReasons[0].UsedByRequests.Should().BeFalse();
        response.SkippedTubeTypeReasons[0].UsedByQualityControls.Should().BeTrue();

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockSender.Verify(s => s.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_315")]
    [Trait("ModuleRequirement", "MR_5478")]
    [Trait("ProductRequirement", "PR_316")]
    [Trait("ModuleRequirement", "MR_6034")]
    [Fact]
    public async Task DeletePages_WithTubeTypeUsedByAnyQualityControl_WhenDefaultConfigurationMissing_ShouldUseDbDefaultsAndSkipWithQualityControlReason()
    {
        var tubeEntryKey = "Samples:TubeTypes:TubeTypeList:0";
        var tubeDefinitionId = "3";

        var settings = new List<Setting>
        {
            new() { Key = "Samples:TubeTypes:NormalTubeDefinitionId", Value = "1" },
            new() { Key = "Samples:TubeTypes:PediatricTubeDefinitionId", Value = "2" },
            new() { Key = $"{tubeEntryKey}:TubeDefinitionId", Value = tubeDefinitionId },
            new() { Key = $"{tubeEntryKey}:Name", Value = "Tube" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        _mockSender
            .Setup(s => s.Send(It.IsAny<IsTubeTypeUsedByAnyRequestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockSender
            .Setup(s => s.Send(It.IsAny<IsTubeTypeUsedByAnyQualityControlQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut(mockDbContext, new Mock<IConfiguration>());

        var response = await sut.Handle(new DeletePagesCommand([tubeEntryKey]), TestContext.Current.CancellationToken);

        response.Deleted.Should().Be(0);
        response.Skipped.Should().Be(1);
        response.SkippedIds.Should().BeEmpty();
        response.SkippedPageListEntryKeys.Should().ContainSingle().Which.Should().Be(tubeEntryKey);
        response.SkippedTubeTypeReasons.Should().ContainSingle();
        response.SkippedTubeTypeReasons[0].PageListEntryKey.Should().Be(tubeEntryKey);
        response.SkippedTubeTypeReasons[0].UsedByRequests.Should().BeFalse();
        response.SkippedTubeTypeReasons[0].UsedByQualityControls.Should().BeTrue();

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockSender.Verify(s => s.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_315")]
    [Trait("ModuleRequirement", "MR_5478")]
    [Trait("ProductRequirement", "PR_316")]
    [Trait("ModuleRequirement", "MR_6034")]
    [Fact]
    public async Task DeletePages_WithDefaultTubeTypeUsedByQualityControl_ShouldSkipWithQualityControlReason()
    {
        var tubeEntryKey = "Samples:TubeTypes:TubeTypeList:0";
        var tubeDefinitionId = "1";

        var settings = new List<Setting>
        {
            new() { Key = "Samples:TubeTypes:NormalTubeDefinitionId", Value = tubeDefinitionId },
            new() { Key = "Samples:TubeTypes:PediatricTubeDefinitionId", Value = "2" },
            new() { Key = $"{tubeEntryKey}:TubeDefinitionId", Value = tubeDefinitionId },
            new() { Key = $"{tubeEntryKey}:Name", Value = "Normal tube" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["Samples:TubeTypes:NormalTubeDefinitionId"]).Returns(tubeDefinitionId);
        mockConfiguration.Setup(c => c["Samples:TubeTypes:PediatricTubeDefinitionId"]).Returns("2");

        _mockSender
            .Setup(s => s.Send(It.IsAny<IsTubeTypeUsedByAnyRequestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockSender
            .Setup(s => s.Send(It.IsAny<IsTubeTypeUsedByAnyQualityControlQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut(mockDbContext, mockConfiguration);

        var response = await sut.Handle(new DeletePagesCommand([tubeEntryKey]), TestContext.Current.CancellationToken);

        response.Deleted.Should().Be(0);
        response.Skipped.Should().Be(1);
        response.SkippedIds.Should().BeEmpty();
        response.SkippedPageListEntryKeys.Should().ContainSingle().Which.Should().Be(tubeEntryKey);
        response.SkippedTubeTypeReasons.Should().ContainSingle();
        response.SkippedTubeTypeReasons[0].PageListEntryKey.Should().Be(tubeEntryKey);
        response.SkippedTubeTypeReasons[0].UsedByRequests.Should().BeFalse();
        response.SkippedTubeTypeReasons[0].UsedByQualityControls.Should().BeTrue();

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockSender.Verify(s => s.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_315")]
    [Trait("ModuleRequirement", "MR_5478")]
    [Trait("ProductRequirement", "PR_316")]
    [Trait("ModuleRequirement", "MR_6034")]
    [Fact]
    public async Task DeletePages_WithTubeTypeUsedByBothRequestAndQualityControl_ShouldSkipDeletion()
    {
        var tubeEntryKey = "Samples:TubeTypes:TubeTypeList:0";
        var tubeDefinitionId = "3";

        var settings = new List<Setting>
        {
            new() { Key = "Samples:TubeTypes:NormalTubeDefinitionId", Value = "1" },
            new() { Key = "Samples:TubeTypes:PediatricTubeDefinitionId", Value = "2" },
            new() { Key = $"{tubeEntryKey}:TubeDefinitionId", Value = tubeDefinitionId },
            new() { Key = $"{tubeEntryKey}:Name", Value = "Tube" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["Samples:TubeTypes:NormalTubeDefinitionId"]).Returns("1");
        mockConfiguration.Setup(c => c["Samples:TubeTypes:PediatricTubeDefinitionId"]).Returns("2");

        _mockSender
            .Setup(s => s.Send(It.IsAny<IsTubeTypeUsedByAnyRequestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockSender
            .Setup(s => s.Send(It.IsAny<IsTubeTypeUsedByAnyQualityControlQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut(mockDbContext, mockConfiguration);

        var response = await sut.Handle(new DeletePagesCommand([tubeEntryKey]), TestContext.Current.CancellationToken);

        response.Deleted.Should().Be(0);
        response.Skipped.Should().Be(1);
        response.SkippedIds.Should().BeEmpty();
        response.SkippedPageListEntryKeys.Should().ContainSingle().Which.Should().Be(tubeEntryKey);
        response.SkippedTubeTypeReasons.Should().ContainSingle();
        response.SkippedTubeTypeReasons[0].PageListEntryKey.Should().Be(tubeEntryKey);
        response.SkippedTubeTypeReasons[0].UsedByRequests.Should().BeTrue();
        response.SkippedTubeTypeReasons[0].UsedByQualityControls.Should().BeTrue();

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockSender.Verify(s => s.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_315")]
    [Trait("ModuleRequirement", "MR_5478")]
    [Fact]
    public async Task DeletePages_WithOneDefaultAndOneNonDefaultTubeType_ShouldDeleteOnlyNonDefault()
    {
        var defaultEntryKey = "Samples:TubeTypes:TubeTypeList:0";
        var nonDefaultEntryKey = "Samples:TubeTypes:TubeTypeList:1";

        // Defaults in DB
        var normalDefaultId = "1";
        var pediatricDefaultId = "2";

        // DB values per entry
        var defaultTubeDefinitionId = "1";   // matches normal default -> must be skipped
        var nonDefaultTubeDefinitionId = "3"; // not default -> must be deleted

        var settings = new List<Setting>
        {
            // Default entry settings (should NOT be removed)
            new() { Key = $"{defaultEntryKey}:TubeDefinitionId", Value = defaultTubeDefinitionId },
            new() { Key = $"{defaultEntryKey}:Name", Value = "Default tube" },

            // Non-default entry settings (should be removed)
            new() { Key = $"{nonDefaultEntryKey}:TubeDefinitionId", Value = nonDefaultTubeDefinitionId },
            new() { Key = $"{nonDefaultEntryKey}:Name", Value = "Other tube" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["Samples:TubeTypes:NormalTubeDefinitionId"]).Returns(normalDefaultId);
        mockConfiguration.Setup(c => c["Samples:TubeTypes:PediatricTubeDefinitionId"]).Returns(pediatricDefaultId);
        var sut = CreateSut(mockDbContext, mockConfiguration);

        var command = new DeletePagesCommand(new[] { defaultEntryKey, nonDefaultEntryKey });

        var response = await sut.Handle(command, TestContext.Current.CancellationToken);

        response.Deleted.Should().Be(1);
        response.Skipped.Should().Be(1);
        response.SkippedPageListEntryKeys.Should().ContainSingle().Which.Should().Be(defaultEntryKey);
        response.SkippedTubeTypeReasons.Should().ContainSingle();
        response.SkippedTubeTypeReasons[0].IsDefaultTubeType.Should().BeTrue();

        // Only the non-default entry's 2 settings should be removed
        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Exactly(2));
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
