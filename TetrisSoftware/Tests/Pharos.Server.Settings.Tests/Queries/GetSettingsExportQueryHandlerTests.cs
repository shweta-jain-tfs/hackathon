using System.Collections.Immutable;
using System.Reflection;
using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application;
using Pharos.Server.Application.AuditTrail;
using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.Application.Identity;
using Pharos.Server.Application.Settings.Instrument;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Queries;
using Pharos.Server.Settings.Domain;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;
using EventId = Pharos.Server.Application.AuditTrail.Enums.EventId;

namespace Pharos.Server.Settings.Queries;

public class GetSettingsExportQueryHandlerTests : IDisposable
{
    private readonly Mock<ISettingsDbContext> _mockDbContext;
    private readonly GetSettingsExportQueryHandler _handler;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ISender> _mockSender;

    public GetSettingsExportQueryHandlerTests()
    {
        _mockDbContext = new Mock<ISettingsDbContext>();
        var mockLogger = new Mock<ILogger<GetSettingsExportQueryHandler>>();
        _mockSender = new();
        _mockCurrentUserService = new();
        _handler = new GetSettingsExportQueryHandler(_mockDbContext.Object, new Mock<IPharosVersionService>().Object, mockLogger.Object, _mockSender.Object, _mockCurrentUserService.Object);

        var assemblies = Modules.ModuleList.Select(m => m.Assembly)
            .Append(typeof(InstrumentSettings).Assembly)
            .ToImmutableList();
        BaseDefaultSettingsProvider.Initialize(assemblies);
    }

    public void Dispose()
    {
        BaseSettingsPagesProvider.Initialize([]);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3655")]
    public async Task Handle_ShouldReturnSettingsExport()
    {
        // Arrange
        var settings = new List<Setting>
        {
            new()
                { Key = "Setting1", Value = "Value1" },
            new()
                { Key = "Setting2", Value = "Value2" }
        };

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var request = new GetSettingsExportQuery();

        // Act
        var result = await _handler.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        var resultLines = result.Trim().Split(Environment.NewLine);
        Assert.Single(resultLines, "Setting1,Value1");
        Assert.Single(resultLines, "Setting2,Value2");
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5999")]
    public async Task Handle_ShouldNotExportInstrumentSpecificSettings()
    {
        var settings = new List<Setting>
        {
            new() { Key = "Instrument:EnableAutomaticPowerSave", Value = "True" },
            new() { Key = "Instrument:PowerSaveTimeoutMinutes", Value = "42" },
            new() { Key = "Instrument:Instruments:0:Name", Value = "Instrument A" },
            new() { Key = "Instrument:Instruments:0:SerialNumber", Value = "SN-1" },
            new() { Key = "Instrument:Instruments:0:Waste:MaximumLiquidWasteFillingVolume", Value = "7" },
            new() { Key = "Setting1", Value = "Value1" }
        };

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var result = await _handler.Handle(new GetSettingsExportQuery(), TestContext.Current.CancellationToken);

        result.Should().NotContain("Instrument:Instruments:0:Name");
        result.Should().NotContain("Instrument:Instruments:0:SerialNumber");
        result.Should().NotContain("Instrument:Instruments:0:Waste:MaximumLiquidWasteFillingVolume");
        result.Should().Contain("Instrument:EnableAutomaticPowerSave,True");
        result.Should().Contain("Instrument:PowerSaveTimeoutMinutes,42");
        result.Should().Contain("Setting1,Value1");
    }

    [Fact]
    public async Task Handle_ShouldNotExportActionButtons()
    {
        var settings = new List<Setting>
        {
            new() { Key = "Configuration:ImportExport:ImportButton", Value = "True" },
            new() { Key = "Configuration:ImportExport:ExportButton", Value = "True" },
            new() { Key = "Instrument:EnableAutomaticPowerSave", Value = "True" }
        };

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var result = await _handler.Handle(new GetSettingsExportQuery(), TestContext.Current.CancellationToken);

        result.Should().NotContain("Configuration:ImportExport:ImportButton");
        result.Should().NotContain("Configuration:ImportExport:ExportButton");
        result.Should().Contain("Instrument:EnableAutomaticPowerSave,True");
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5999")]
    public async Task Handle_ShouldExportDefaultInstrumentGeneralSettings_WhenInstrumentSpecificSettingsAreExcluded()
    {
        var settings = new List<Setting>
        {
            new() { Key = "Instrument:Instruments:0:Name", Value = "Instrument A" },
            new() { Key = "Instrument:Instruments:0:SerialNumber", Value = "SN-1" }
        };

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        var result = await _handler.Handle(new GetSettingsExportQuery(), TestContext.Current.CancellationToken);

        result.Should().Contain("Instrument:EnableAutomaticPowerSave,False");
        result.Should().Contain("Instrument:PowerSaveTimeoutMinutes,15");
        result.Should().NotContain("Instrument:Instruments:0:Name");
        result.Should().NotContain("Instrument:Instruments:0:SerialNumber");
    }

    [Fact]
    public async Task CreateAuditTrailRecord_WhenSettingsExported()
    {
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
        _mockSender.Setup(s => s.Send(It.IsAny<GetApplicationUserByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUserDto
            {
                UserName = "testUser"
            });
        _mockSender.Setup(s => s.Send(It.IsAny<CreateAuditTrailRecordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var createAuditTrailRecordMethod = typeof(GetSettingsExportQueryHandler).GetMethod("CreateAuditTrailRecord", BindingFlags.NonPublic | BindingFlags.Instance);
        if (createAuditTrailRecordMethod == null)
        {
            throw new InvalidOperationException("Method not found.");
        }

        await (Task)createAuditTrailRecordMethod.Invoke(_handler, [TestContext.Current.CancellationToken])!;

        _mockSender.Verify(s => s.Send(It.IsAny<GetApplicationUserByIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSender.Verify(s => s.Send(It.Is<CreateAuditTrailRecordCommand>(cmd =>
            cmd.EventId == EventId.SettingsExported &&
            cmd.EventData.Any(data =>
                data.Data.ContainsKey(EventDataFields.Username.ToString()) &&
                data.Data[EventDataFields.Username.ToString()].GetString() == "testUser") &&
            cmd.Level == EventLevel.Information
            ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
