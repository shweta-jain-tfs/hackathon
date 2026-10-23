using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.AuditTrail;
using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Settings.Application.Commands.DeleteSettings;
using Pharos.Server.Settings.Application.Commands.NotifySettingsChanged;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Domain;
using EventId = Pharos.Server.Application.AuditTrail.Enums.EventId;

namespace Pharos.Server.Settings.Commands;

public class DeleteSettingsCommandTests
{
    private readonly Mock<ISettingsDbContext> _mockDbContext;
    private DeleteSettingsCommand? _request;
    private readonly DeleteSettingsCommandHandler _sut;
    private readonly Mock<ILogger<DeleteSettingsCommandHandler>> _mockLogger = new();
    private readonly Mock<ISender> _mockSender = new();

    public DeleteSettingsCommandTests()
    {
        var entry1 = new Setting
        {
            Key = "Table:0:Entry1",
            Value = "10"
        };
        var entry2 = new Setting
        {
            Key = "Table:0:Entry2",
            Value = "settingValue2"
        };
        var entry3 = new Setting
        {
            Key = "Table:0:Entry3",
            Value = "false"
        };

        List<Setting> settings = [
            entry1,
            entry2,
            entry3
        ];

        _mockDbContext = new Mock<ISettingsDbContext>();
        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);
        _sut = new DeleteSettingsCommandHandler(_mockDbContext.Object, _mockLogger.Object, _mockSender.Object);
    }

    [Fact]
    public async Task ShouldRemoveEntryFromDb()
    {
        //Arrange
        var entriesToDelete = new List<string>
        {
            "Table:0:Entry1",
            "Table:0:Entry2",
            "Table:0:Entry3",
        };

        _request = new DeleteSettingsCommand(entriesToDelete.ToArray());

        //Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        //Assert
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Exactly(3));
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);

    }

    [Fact]
    public async Task ShouldThrowCorrectErrorWhenRemovingEntryNotInTheDb()
    {
        //Arrange
        var entriesToDelete = new List<string>
        {
            "Table:1:Entry1",
            "Table:1:Entry2",
            "Table:1:Entry3",
        };

        _request = new DeleteSettingsCommand(entriesToDelete.ToArray());

        //Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _sut.Handle(_request!, TestContext.Current.CancellationToken);
        });
        Assert.Equal("No settings found! Keys: Table:1:Entry1,Table:1:Entry2,Table:1:Entry3", exception.Message);


        //Assert
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);

    }

    [Fact]
    public async Task ShouldCallAuditTrailEventWhenSettingsAreDeleted()
    {
        // Arrange
        var entriesToDelete = new List<string>
        {
            "Table:0:Entry1",
            "Table:0:Entry2",
            "Table:0:Entry3",
        };

        _request = new DeleteSettingsCommand(entriesToDelete.ToArray());

        // Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        // Assert
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Exactly(3));
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
                    entriesToDelete.All(key => cmd.DeletedKeys.Contains(key))),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteSettings_WithSingleLisChannel_ShouldRemoveAllRelatedSettingsFromDb()
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
        var sut = new DeleteSettingsCommandHandler(mockDbContext.Object, _mockLogger.Object, _mockSender.Object);

        var keysToDelete = settings.Select(s => s.Key).ToArray();
        var command = new DeleteSettingsCommand(keysToDelete);

        await sut.Handle(command, TestContext.Current.CancellationToken);

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Exactly(5));
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSettings_WithMultipleLisChannels_ShouldRemoveAllRelatedSettingsFromDb()
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
        var sut = new DeleteSettingsCommandHandler(mockDbContext.Object, _mockLogger.Object, _mockSender.Object);

        var keysToDelete = settings.Select(s => s.Key).ToArray();
        var command = new DeleteSettingsCommand(keysToDelete);

        await sut.Handle(command, TestContext.Current.CancellationToken);

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Exactly(6));
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSettings_WithNestedSettings_ShouldRemoveAllNestedSettingsFromDb()
    {
        var lisChannelKey = "LIS:LisChannels:0";
        var settings = new List<Setting>
        {
            new() { Key = $"{lisChannelKey}:Name", Value = "Test LIS" },
            new() { Key = $"{lisChannelKey}:AstmSettings:CommunicationSettings:TcpSettings:IpAddress", Value = "127.0.0.1" },
            new() { Key = $"{lisChannelKey}:AstmSettings:CommunicationSettings:TcpSettings:TcpPort", Value = "1001" },
            new() { Key = $"{lisChannelKey}:AstmSettings:ExportSettings:SkipExportOfPatientInformation", Value = "false" },
            new() { Key = $"{lisChannelKey}:ImportExportSettings:ImportEnabled", Value = "true" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);
        var sut = new DeleteSettingsCommandHandler(mockDbContext.Object, _mockLogger.Object, _mockSender.Object);

        var keysToDelete = settings.Select(s => s.Key).ToArray();
        var command = new DeleteSettingsCommand(keysToDelete);

        await sut.Handle(command, TestContext.Current.CancellationToken);

        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Exactly(5));
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSettings_WithPartialMatch_ShouldOnlyRemoveMatchingSettings()
    {
        var lisChannelKey = "LIS:LisChannels:0";
        var settings = new List<Setting>
        {
            new() { Key = $"{lisChannelKey}:Name", Value = "Channel 0" },
            new() { Key = $"{lisChannelKey}:IsEnabled", Value = "true" },
            new() { Key = "LIS:LisChannels:1:Name", Value = "Channel 1" },
            new() { Key = "LIS:LisChannels:1:IsEnabled", Value = "false" },
            new() { Key = "LIS:OtherSetting", Value = "Other" }
        };

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);
        var sut = new DeleteSettingsCommandHandler(mockDbContext.Object, _mockLogger.Object, _mockSender.Object);

        var keysToDelete = settings.Where(s => s.Key.StartsWith(lisChannelKey)).Select(s => s.Key).ToArray();
        var command = new DeleteSettingsCommand(keysToDelete);

        await sut.Handle(command, TestContext.Current.CancellationToken);

        // Should only remove the 2 settings for LisChannels:0, not LisChannels:1 or other settings
        mockDbContext.Verify(m => m.Settings.Remove(It.Is<Setting>(s => s.Key.StartsWith(lisChannelKey))), Times.Exactly(2));
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
