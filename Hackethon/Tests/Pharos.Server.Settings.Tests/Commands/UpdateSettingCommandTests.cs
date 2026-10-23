using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.AuditTrail;
using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Settings.Application.Commands;
using Pharos.Server.Settings.Application.Commands.NotifySettingsChanged;
using Pharos.Server.Settings.Application.Commands.UpdateSettings;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Validation;
using Pharos.Server.Settings.Domain;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Commands;

public class UpdateSettingCommandTests
{
    private readonly Mock<ISettingsDbContext> _mockDbContext;
    private UpdateSettingCommand? _request;
    private readonly UpdateSettingCommandHandler _sut;
    private readonly Mock<ILogger<UpdateSettingCommandHandler>> _mockLogger = new();
    private readonly Mock<ISender> _mockSender;
    private readonly Mock<IPageListMaxItemsValidator> _mockValidator = new();
    private readonly Mock<IAbsoluteDirectoryWriteAccessValidator> _mockWriteAccessValidator = new();

    public UpdateSettingCommandTests()
    {
        var setting1 = new Setting()
        {
            Key = "Setting1",
            LastModified = DateTimeOffset.FromUnixTimeSeconds(1725533542),
            LastModifiedBy = Guid.NewGuid(),
            Value = "10"
        };
        var setting2 = new Setting()
        {
            Key = "Setting2",
            LastModified = DateTimeOffset.FromUnixTimeSeconds(1725533542),
            LastModifiedBy = Guid.NewGuid(),
            Value = "settingValue2"
        };
        var setting3 = new Setting()
        {
            Key = "Setting3",
            LastModified = DateTimeOffset.FromUnixTimeSeconds(1725533542),
            LastModifiedBy = Guid.NewGuid(),
            Value = "false"
        };

        List<Setting> settings =
        [
            setting1,
            setting2,
            setting3
        ];
        _mockSender = new Mock<ISender>();
        _mockDbContext = new Mock<ISettingsDbContext>();
        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        // Setup validator to always return success by default
        _mockValidator.Setup(v => v.ValidateCanAddAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PageListMaxItemsValidationResult.Success());
        _mockValidator.Setup(v => v.ValidateExistingPageLists(It.IsAny<Page>(), It.IsAny<IEnumerable<string>>()))
            .Returns(PageListMaxItemsValidationResult.Success());
        _mockWriteAccessValidator
            .Setup(v => v.Validate(It.IsAny<string?>()))
            .Returns(AbsoluteDirectoryWriteAccessValidationResult.Success());

        _sut = new UpdateSettingCommandHandler(_mockDbContext.Object, _mockLogger.Object, _mockSender.Object, _mockValidator.Object, _mockWriteAccessValidator.Object);
        BaseDefaultSettingsProvider.Initialize([]);
    }

    private void InitializeUpdateSettingCommandRequest(string key1, string value1, string key2, string value2)
    {
        var settingToSave = new Page()
        {
            Children =
            [
                new Option()
                {
                    Key = key1,
                    DefaultValue = "0",
                    Value = value1
                },

                new Group()
                {
                    Children =
                    [
                        new Option()
                        {
                            Key = key2,
                            DefaultValue = "0",
                            Value = value2
                        }
                    ]
                }
            ]
        };

        _request = new("", settingToSave);
    }

    private void InitializeIntegerSetting(string key, string value, bool required, bool unique, string minValue = "-100", string maxValue = "100")
    {
        _request = InitializeSetting(key, value, "0", required, unique, OptionType.Integer, minValue, maxValue);
    }

    private static UpdateSettingCommand InitializeSetting(string key, string value, string defaultValue, bool required, bool unique, OptionType type, string minValue, string maxValue)
    {
        var settingToSave = new Page()
        {
            Children =
            [
                new Option()
                {
                    Key = key,
                    DefaultValue = defaultValue,
                    Value = value,
                    Required = required,
                    Unique = unique,
                    MinValue = minValue,
                    MaxValue = maxValue,
                    Type = type,
                },
            ]
        };

        return new("", settingToSave);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3643")]
    public async Task Should_add_new_setting_to_db()
    {
        //Arrange
        InitializeUpdateSettingCommandRequest("NewSetting1", "1", "NewSetting2", "2");

        //Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        //Assert
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Exactly(2));
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3642")]
    public async Task Should_remove_setting_updated_to_default_from_db()
    {
        //Arrange
        InitializeUpdateSettingCommandRequest("Setting1", "0", "Setting2", "0");

        //Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        //Assert
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Exactly(2));
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3643")]
    public async Task Should_update_setting_in_db()
    {
        //Arrange
        InitializeUpdateSettingCommandRequest("Setting1", "5", "Setting2", "updatedValue");

        //Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        //Assert
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Exactly(2));
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
    }

    [Fact]
    public async Task Should_add_protected_setting_to_protected_value()
    {
        // Arrange
        _request = InitializeSetting("ProtectedSetting", "secret", string.Empty, false, false, OptionType.String, string.Empty, string.Empty);
        _request.SettingToSave!.Children[0].As<Option>().IsEncrypted = true;

        // Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        // Assert
        _mockDbContext.Verify(m => m.Settings.Add(It.Is<Setting>(s =>
            s.Key == "ProtectedSetting" &&
            s.Value == null &&
            s.ProtectedValue == "secret")), Times.Once);
    }

    [Fact]
    public async Task Should_update_protected_setting_in_protected_value()
    {
        // Arrange
        var existingSettings = new List<Setting>
        {
            new()
            {
                Key = "ProtectedSetting",
                ProtectedValue = "old-secret"
            }
        };
        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(existingSettings);

        _request = InitializeSetting("ProtectedSetting", "new-secret", string.Empty, false, false, OptionType.String, string.Empty, string.Empty);
        _request.SettingToSave!.Children[0].As<Option>().IsEncrypted = true;

        // Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        // Assert
        _mockDbContext.Verify(m => m.Settings.Update(It.Is<Setting>(s =>
            s.Key == "ProtectedSetting" &&
            s.Value == null &&
            s.ProtectedValue == "new-secret")), Times.Once);
    }

    [Fact]
    public async Task Should_move_existing_protected_setting_from_value_to_protected_value()
    {
        // Arrange
        var existingSettings = new List<Setting>
        {
            new()
            {
                Key = "ProtectedSetting",
                Value = "secret"
            }
        };
        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(existingSettings);

        _request = InitializeSetting("ProtectedSetting", "secret", string.Empty, false, false, OptionType.String, string.Empty, string.Empty);
        _request.SettingToSave!.Children[0].As<Option>().IsEncrypted = true;

        // Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        // Assert
        _mockDbContext.Verify(m => m.Settings.Update(It.Is<Setting>(s =>
            s.Key == "ProtectedSetting" &&
            s.Value == null &&
            s.ProtectedValue == "secret")), Times.Once);
    }

    [Fact]
    public async Task Should_not_save_provider_managed_readonly_name()
    {
        // Arrange
        var nameSetting = new Setting { Key = "ProviderManagedSave:Entries:0:Name", Value = "Old instrument name" };
        var entryKeySetting = new Setting { Key = "ProviderManagedSave:Entries:0:EntryKey", Value = "instrument-1" };
        List<Setting> settings = [nameSetting, entryKeySetting];

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        BaseDefaultSettingsProvider.Initialize([GetType().Assembly], _ => [typeof(ProviderManagedSaveSettings)]);
        var sut = new UpdateSettingCommandHandler(mockDbContext.Object, _mockLogger.Object, _mockSender.Object, _mockValidator.Object, _mockWriteAccessValidator.Object);
        var request = new UpdateSettingCommand(
            string.Empty,
            new Page
            {
                Key = "ProviderManagedSave:Entries:0",
                Children =
                [
                    new Option
                    {
                        Key = "ProviderManagedSave:Entries:0:Name",
                        Value = "New instrument name",
                        DefaultValue = string.Empty,
                        Type = OptionType.String,
                        MaxLength = 100,
                        IsNameColumn = true,
                        IsReadOnly = true
                    },
                    new Option
                    {
                        Key = "ProviderManagedSave:Entries:0:EntryKey",
                        Value = "instrument-1",
                        DefaultValue = string.Empty,
                        Type = OptionType.String,
                        MaxLength = 100
                    },
                    new Option
                    {
                        Key = "ProviderManagedSave:Entries:0:Enabled",
                        Value = "True",
                        DefaultValue = "False",
                        Type = OptionType.Boolean
                    }
                ]
            },
            IsPageListUpdate: true);

        // Act
        await sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        mockDbContext.Verify(m => m.Settings.Remove(nameSetting), Times.Once);
        mockDbContext.Verify(m => m.Settings.Add(It.Is<Setting>(s => s.Key == "ProviderManagedSave:Entries:0:Name")), Times.Never);
        mockDbContext.Verify(m => m.Settings.Update(It.Is<Setting>(s => s.Key == "ProviderManagedSave:Entries:0:Name")), Times.Never);
        mockDbContext.Verify(m => m.Settings.Add(It.Is<Setting>(s => s.Key == "ProviderManagedSave:Entries:0:Enabled" && s.Value == "True")), Times.Once);
        mockDbContext.Verify(m => m.Settings.Update(It.Is<Setting>(s => s.Key == "ProviderManagedSave:Entries:0:EntryKey")), Times.Never);
    }

    [Fact]
    public async Task Should_not_save_synthetic_provider_managed_name()
    {
        // Arrange
        var entryKeySetting = new Setting { Key = "ProviderManagedWithoutNameSave:Entries:0:EntryKey", Value = "instrument-1" };
        List<Setting> settings = [entryKeySetting];

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        BaseDefaultSettingsProvider.Initialize([GetType().Assembly], _ => [typeof(ProviderManagedWithoutNameSaveSettings)]);
        var sut = new UpdateSettingCommandHandler(mockDbContext.Object, _mockLogger.Object, _mockSender.Object, _mockValidator.Object, _mockWriteAccessValidator.Object);
        var request = new UpdateSettingCommand(
            string.Empty,
            new Page
            {
                Key = "ProviderManagedWithoutNameSave:Entries:0",
                Children =
                [
                    new Option
                    {
                        Key = "ProviderManagedWithoutNameSave:Entries:0:Name",
                        Value = "Provider display name",
                        DefaultValue = string.Empty,
                        Type = OptionType.String,
                        MaxLength = 100,
                        IsNameColumn = true,
                        IsReadOnly = true
                    },
                    new Option
                    {
                        Key = "ProviderManagedWithoutNameSave:Entries:0:EntryKey",
                        Value = "instrument-1",
                        DefaultValue = string.Empty,
                        Type = OptionType.String,
                        MaxLength = 100
                    },
                    new Option
                    {
                        Key = "ProviderManagedWithoutNameSave:Entries:0:Enabled",
                        Value = "True",
                        DefaultValue = "False",
                        Type = OptionType.Boolean
                    }
                ]
            },
            IsPageListUpdate: true);

        // Act
        await sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        mockDbContext.Verify(m => m.Settings.Add(It.Is<Setting>(s => s.Key == "ProviderManagedWithoutNameSave:Entries:0:Name")), Times.Never);
        mockDbContext.Verify(m => m.Settings.Update(It.Is<Setting>(s => s.Key == "ProviderManagedWithoutNameSave:Entries:0:Name")), Times.Never);
        mockDbContext.Verify(m => m.Settings.Add(It.Is<Setting>(s => s.Key == "ProviderManagedWithoutNameSave:Entries:0:Enabled" && s.Value == "True")), Times.Once);
        mockDbContext.Verify(m => m.Settings.Update(It.Is<Setting>(s => s.Key == "ProviderManagedWithoutNameSave:Entries:0:EntryKey")), Times.Never);
    }

    [Fact]
    public async Task Should_save_provider_managed_entry_without_existing_settings()
    {
        // Arrange
        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(new List<Setting>());

        BaseDefaultSettingsProvider.Initialize([GetType().Assembly], _ => [typeof(ProviderManagedWithoutNameSaveSettings)]);
        var sut = new UpdateSettingCommandHandler(mockDbContext.Object, _mockLogger.Object, _mockSender.Object, _mockValidator.Object, _mockWriteAccessValidator.Object);
        var request = new UpdateSettingCommand(
            string.Empty,
            new Page
            {
                Key = "ProviderManagedWithoutNameSave:Entries:0",
                Children =
                [
                    new Option
                    {
                        Key = "ProviderManagedWithoutNameSave:Entries:0:Name",
                        Value = "Provider display name",
                        DefaultValue = string.Empty,
                        Type = OptionType.String,
                        MaxLength = 100,
                        IsNameColumn = true,
                        IsReadOnly = true
                    },
                    new Option
                    {
                        Key = "ProviderManagedWithoutNameSave:Entries:0:EntryKey",
                        Value = "instrument-1",
                        DefaultValue = string.Empty,
                        Type = OptionType.String,
                        MaxLength = 100
                    },
                    new Option
                    {
                        Key = "ProviderManagedWithoutNameSave:Entries:0:Enabled",
                        Value = "True",
                        DefaultValue = "False",
                        Type = OptionType.Boolean
                    }
                ]
            },
            IsPageListUpdate: true);

        // Act
        var result = await sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        result.Success.Should().BeTrue();
        mockDbContext.Verify(m => m.Settings.Add(It.Is<Setting>(s => s.Key == "ProviderManagedWithoutNameSave:Entries:0:Name")), Times.Never);
        mockDbContext.Verify(m => m.Settings.Add(It.Is<Setting>(s => s.Key == "ProviderManagedWithoutNameSave:Entries:0:EntryKey" && s.Value == "instrument-1")), Times.Once);
        mockDbContext.Verify(m => m.Settings.Add(It.Is<Setting>(s => s.Key == "ProviderManagedWithoutNameSave:Entries:0:Enabled" && s.Value == "True")), Times.Once);
    }

    [Fact]
    public async Task Should_save_provider_managed_editable_name()
    {
        // Arrange
        var nameSetting = new Setting { Key = "ProviderManagedEditableSave:Entries:0:Name", Value = "Old name" };
        List<Setting> settings = [nameSetting];

        var mockDbContext = new Mock<ISettingsDbContext>();
        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        BaseDefaultSettingsProvider.Initialize([GetType().Assembly], _ => [typeof(ProviderManagedEditableSaveSettings)]);
        var sut = new UpdateSettingCommandHandler(mockDbContext.Object, _mockLogger.Object, _mockSender.Object, _mockValidator.Object, _mockWriteAccessValidator.Object);
        var request = new UpdateSettingCommand(
            string.Empty,
            new Page
            {
                Key = "ProviderManagedEditableSave:Entries:0",
                Children =
                [
                    new Option
                    {
                        Key = "ProviderManagedEditableSave:Entries:0:Name",
                        Value = "New name",
                        DefaultValue = string.Empty,
                        Type = OptionType.String,
                        MaxLength = 100,
                        IsNameColumn = true
                    }
                ]
            },
            IsPageListUpdate: true);

        // Act
        await sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        mockDbContext.Verify(m => m.Settings.Remove(nameSetting), Times.Never);
        mockDbContext.Verify(m => m.Settings.Update(nameSetting), Times.Once);
        nameSetting.Value.Should().Be("New name");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_565")]
    [Trait("ModuleRequirement", "MR_5377")]
    public async Task MR_5377_Should_throw_object_gone_when_page_list_entry_update_was_deleted()
    {
        // Arrange
        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(new List<Setting>());

        var deletedEntryPage = new Page
        {
            Key = "Lis:LisChannels:0",
            Children =
            [
                new Option
                {
                    Key = "Lis:LisChannels:0:Name",
                    Type = OptionType.String,
                    MaxLength = 100,
                    DefaultValue = string.Empty,
                    Value = "EditedAfterDelete"
                }
            ]
        };

        var request = new UpdateSettingCommand(string.Empty, deletedEntryPage, IsPageListUpdate: true);

        // Act
        var exception = await Assert.ThrowsAsync<ObjectGoneException>(
            () => _sut.Handle(request, CancellationToken.None));

        // Assert
        exception.Message.Should().Be(GetDeletedSettingMessage());
        _mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockSender.Verify(
            sender => sender.Send(It.IsAny<NotifySettingsChangedCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_528")]
    [Trait("ModuleRequirement", "MR_3643")]
    public async Task MR_3643_Should_save_page_list_entry_update_when_entry_still_exists()
    {
        // Arrange
        var existingSetting = new Setting { Key = "Lis:LisChannels:0:Name", Value = "OldName" };
        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(new List<Setting> { existingSetting });

        var entryPage = new Page
        {
            Key = "Lis:LisChannels:0",
            Children =
            [
                new Option
                {
                    Key = "Lis:LisChannels:0:Name",
                    Type = OptionType.String,
                    MaxLength = 100,
                    DefaultValue = string.Empty,
                    Value = "NewName"
                }
            ]
        };

        var request = new UpdateSettingCommand(string.Empty, entryPage, IsPageListUpdate: true);

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        _mockDbContext.Verify(m => m.Settings.Update(existingSetting), Times.Once);
        existingSetting.Value.Should().Be("NewName");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_565")]
    [Trait("ModuleRequirement", "MR_5377")]
    public async Task MR_5377_Should_throw_object_gone_when_child_page_of_page_list_entry_was_deleted()
    {
        // Arrange
        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(new List<Setting>());

        var childPage = new Page
        {
            Key = "Lis:LisChannels:0:Details",
            Children =
            [
                new Option
                {
                    Key = "Lis:LisChannels:0:Details:Description",
                    Type = OptionType.String,
                    MaxLength = 100,
                    DefaultValue = string.Empty,
                    Value = "EditedAfterDelete"
                }
            ]
        };

        var request = new UpdateSettingCommand(string.Empty, childPage, IsPageListUpdate: true);

        // Act
        var exception = await Assert.ThrowsAsync<ObjectGoneException>(
            () => _sut.Handle(request, CancellationToken.None));

        // Assert
        exception.Message.Should().Be(GetDeletedSettingMessage());
        _mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockSender.Verify(
            sender => sender.Send(It.IsAny<NotifySettingsChangedCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_565")]
    [Trait("ModuleRequirement", "MR_3643")]
    public async Task MR_3643_Should_save_child_page_of_page_list_entry_when_entry_still_exists()
    {
        // Arrange
        var existingEntrySetting = new Setting { Key = "Lis:LisChannels:0:Name", Value = "Channel0" };
        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(new List<Setting> { existingEntrySetting });

        var childPage = new Page
        {
            Key = "Lis:LisChannels:0:Details",
            Children =
            [
                new Option
                {
                    Key = "Lis:LisChannels:0:Details:Description",
                    Type = OptionType.String,
                    MaxLength = 100,
                    DefaultValue = string.Empty,
                    Value = "NewDescription"
                }
            ]
        };

        var request = new UpdateSettingCommand(string.Empty, childPage, IsPageListUpdate: true);

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        _mockDbContext.Verify(
            m => m.Settings.Add(It.Is<Setting>(s =>
                s.Key == "Lis:LisChannels:0:Details:Description" &&
                s.Value == "NewDescription")),
            Times.Once);
    }


    [Fact]
    [Trait("ModuleRequirement", "MR_3644")]
    public async Task Should_update_when_required_and_is_valid()
    {
        //Arrange
        InitializeIntegerSetting("Setting1", "1", true, false);

        //Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        //Assert
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Exactly(1));
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3644")]
    public async Task Should_not_update_when_integer_is_less_than_minValue()
    {
        //Arrange
        InitializeIntegerSetting("Setting1", "-101", false, false);

        //Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        //Assert
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3644")]
    public async Task Should_update_when_integer_is_minValid()
    {
        //Arrange
        InitializeIntegerSetting("Setting1", "-100", false, false);

        //Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        //Assert
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Exactly(1));
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
    }

    [Theory]
    [InlineData("Not OK", false, 0)]
    [InlineData("OK", true, 2)]
    [Trait("ModuleRequirement", "MR_3644")]
    public async Task Should_not_update_with_invalid_dynamicDropdown(string dynamicOptionValue, bool expectedSuccess, int numberOfAdds)
    {
        var settingToSave = new Page()
        {
            Label = "Page",
            Key = "TopPage",
            Children =
            [
                new Option()
                {
                    Label = "DynamicDropdown",
                    Key = "TopPage:DynamicDropdown",
                    Type = OptionType.Choice,
                    Value = dynamicOptionValue,
                    DynamicDropdownOrigin = "PageList",
                    DynamicDropdownValueTarget = "Target",
                },
                new PageList()
                {
                    Key = "TopPage:PageList",
                    PageEntries =
                    [
                        new Page()
                        {
                            Children =
                            [
                                new Option()
                                {
                                    Key = "TopPage:PageList:0:Target",
                                    Value = "OK"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        //Arrange
        _request = new("", settingToSave);

        //Act
        var result = await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        //Assert
        Assert.NotNull(result);
        Assert.Equal(expectedSuccess, result.Success);
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Exactly(numberOfAdds));
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3644")]
    public async Task Should_not_update_when_integer_is_more_than_maxValue()
    {
        //Arrange
        InitializeIntegerSetting("Setting1", "101", false, false, "0");

        //Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        //Assert
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3644")]
    public async Task Should_update_when_integer_is_maxValid()
    {
        //Arrange
        InitializeIntegerSetting("Setting1", "100", false, false);

        //Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        //Assert
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Exactly(1));
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3644")]
    public async Task Should_not_add_when_required_and_is_not_valid()
    {
        //Arrange
        InitializeIntegerSetting("Setting4", "0", true, false);

        //Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        //Assert
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3644")]
    public async Task Should_add_when_required_and_is_valid()
    {
        //Arrange
        InitializeIntegerSetting("Setting4", "1", true, false);

        //Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        //Assert
        _mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Exactly(1));
    }

    [Theory]
    [InlineData(5, "6 ", "value_6", 0, 1)]
    [InlineData(5, "6", "value_1", 0, 0)]
    [InlineData(5, "1", "value_123", 1, 0)]
    [InlineData(5, "1", "value_2", 0, 0)]
    public async Task ShouldUpdate_WhenUniqueAndNotFound(int numSettings, string id, string value, int numExpectedUpdateCalls, int numExpectedAddCalls)
    {
        // Arrange
        var settings = GenerateTestSettings(
            numSettings,
            "Request:TubeTypes:TubeTypes",
            [("Name", OptionType.String, false), ("TubeDefinitionId", OptionType.Integer, true)]);
        var (updateHandler, mockDbContext) = GenerateUpdateSettingCommandHandler(settings);

        var key = $"Request:TubeTypes:TubeTypes:{id}:Name";
        var request = InitializeSetting(key, value, string.Empty, false, true, OptionType.String, string.Empty, string.Empty);

        // Act
        await updateHandler.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Exactly(numExpectedUpdateCalls));
        mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Exactly(numExpectedAddCalls));
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3643")]
    public async Task ShouldCreateAuditTrailEvent_WhenSettingsAreModified()
    {
        // Arrange
        InitializeUpdateSettingCommandRequest("Setting1", "5", "Setting2", "updatedValue");

        var existingSettings = new List<Setting>
        {
            new()
                { Key = "Setting1", Value = "1" },
            new()
                { Key = "Setting2", Value = "2" }
        };
        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(existingSettings);

        // Act
        await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        // Assert
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Exactly(2));
        _mockSender.Verify(s => s.Send(It.Is<CreateAuditTrailRecordCommand>(cmd =>
            cmd.EventId == Server.Application.AuditTrail.Enums.EventId.SettingsChanged &&
            cmd.Level == EventLevel.Information
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2136")]
    [Trait("ModuleRequirement", "MR_5805")]
    [Fact]
    public async Task Should_return_write_access_error_when_absolute_directory_path_is_not_writable()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "pharos-no-write-access");
        var key = "Result:Export:AnalyticalRunExportPath";

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(new List<Setting>());
        _mockWriteAccessValidator
            .Setup(v => v.Validate(path))
            .Returns(AbsoluteDirectoryWriteAccessValidationResult.Failure(path, "Access denied."));

        var request = new UpdateSettingCommand(
            string.Empty,
            new Page
            {
                Children =
                [
                    new Option
                    {
                        Key = key,
                        Type = OptionType.String,
                        DefaultValue = string.Empty,
                        Value = path,
                        IsAbsoluteDirectoryPath = true
                    }
                ]
            });

        // Act
        var result = await _sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal(SettingUpdateError.AbsoluteDirectoryWriteAccessDenied, error.Error);
        Assert.Equal(key, error.Key);
        Assert.Equal(path, error.Value);
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_2136")]
    [Trait("ModuleRequirement", "MR_5805")]
    [Fact]
    public async Task Should_not_validate_write_access_when_absolute_directory_path_is_unchanged()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "pharos-existing-path");
        var key = "Result:Export:AnalyticalRunExportPath";
        var existingSettings = new List<Setting>
        {
            new() { Key = key, Value = path }
        };

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(existingSettings);

        var request = new UpdateSettingCommand(
            string.Empty,
            new Page
            {
                Children =
                [
                    new Option
                    {
                        Key = key,
                        Type = OptionType.String,
                        DefaultValue = string.Empty,
                        Value = path,
                        IsAbsoluteDirectoryPath = true
                    }
                ]
            });

        // Act
        var result = await _sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.Success);
        _mockWriteAccessValidator.Verify(v => v.Validate(It.IsAny<string?>()), Times.Never);
    }

    private static List<Setting> GenerateTestSettings(int numSettings, string prefixSegments, (string, OptionType, bool)[] properties)
    {
        var settings = new List<Setting>();
        for (var i = 1; i < numSettings + 1; i++)
        {
            settings.AddRange(properties.Select(property => new Setting
            {
                Key = $"{prefixSegments}:{i}:{property.Item1}",
                Value = property.Item2 switch
                {
                    OptionType.Boolean => "true",
                    OptionType.Integer => $"{i}",
                    OptionType.Double => "10.0",
                    _ => $"value_{i}"
                }
            }));
        }
        return settings;
    }

    private static (UpdateSettingCommandHandler, Mock<ISettingsDbContext>) GenerateUpdateSettingCommandHandler(List<Setting> settings)
    {
        var mockLogger = Mock.Of<ILogger<UpdateSettingCommandHandler>>();
        var mockDbContext = new Mock<ISettingsDbContext>();
        var mockSender = new Mock<ISender>();
        var mockValidator = new Mock<IPageListMaxItemsValidator>();
        var mockWriteAccessValidator = new Mock<IAbsoluteDirectoryWriteAccessValidator>();

        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        // Setup validator to always return success by default
        mockValidator.Setup(v => v.ValidateCanAddAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PageListMaxItemsValidationResult.Success());
        mockValidator.Setup(v => v.ValidateExistingPageLists(It.IsAny<Page>(), It.IsAny<IEnumerable<string>>()))
            .Returns(PageListMaxItemsValidationResult.Success());
        mockWriteAccessValidator.Setup(v => v.Validate(It.IsAny<string?>())).Returns(AbsoluteDirectoryWriteAccessValidationResult.Success());

        var updateHandler = new UpdateSettingCommandHandler(mockDbContext.Object, mockLogger, mockSender.Object, mockValidator.Object, mockWriteAccessValidator.Object);
        return (updateHandler, mockDbContext);
    }

    #region MaxItems Validation Tests

    [Fact]
    public async Task Should_fail_add_when_max_items_exceeded_for_new_pagelist_entry()
    {
        // Arrange - DB already has 10 items in the PageList (max reached)
        var existingSettings = Enumerable.Range(0, 10)
            .Select(i => new Setting { Key = $"Lis:LisChannels:{i}:Name", Value = $"Channel{i}" })
            .ToList();

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(existingSettings);

        var newEntryPage = new Page
        {
            Key = "Lis:LisChannels:10",
            Children =
            [
                new Option
                {
                    Key = "Lis:LisChannels:10:Name",
                    Type = OptionType.String,
                    MaxLength = 100,
                    DefaultValue = string.Empty,
                    Value = "Channel10"
                }
            ]
        };

        _mockValidator
            .Setup(v => v.ValidateCanAddAsync("Lis:LisChannels", "Lis", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PageListMaxItemsValidationResult.Failure("Max items reached", 10, 10));

        _request = new(string.Empty, newEntryPage);

        // Act
        var result = await _sut.Handle(_request!, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Error == SettingUpdateError.MaxItemsExceeded);
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockValidator.Verify(v => v.ValidateCanAddAsync("Lis:LisChannels", "Lis", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_skip_new_entry_validation_when_is_pagelist_update_true()
    {
        // Arrange
        var existingSettings = new List<Setting>
        {
            // Would conflict with the entry prefix if new-entry validation was executed
            new() { Key = "Lis:LisChannels:10:Name", Value = "ExistingChannel10" }
        };

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(existingSettings);

        var pageListEntryUpdate = new Page
        {
            Key = "Lis:LisChannels:10",
            Children =
            [
                new Option
                {
                    Key = "Lis:LisChannels:10:Name",
                    Type = OptionType.String,
                    MaxLength = 100,
                    DefaultValue = string.Empty,
                    Value = "UpdatedChannel10"
                }
            ]
        };

        // If this gets called, the test should fail because IsPageListUpdate=true must skip it
        _mockValidator
            .Setup(v => v.ValidateCanAddAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PageListMaxItemsValidationResult.Failure("Should not be called", 10, 10));

        var request = new UpdateSettingCommand(string.Empty, pageListEntryUpdate, true);

        // Act
        var result = await _sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.Success);
        _mockValidator.Verify(
            v => v.ValidateCanAddAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_fail_with_concurrent_modification_when_new_entry_validation_runs()
    {
        // Arrange
        var existingSettings = new List<Setting>
        {
            // Same exact entry key exists already -> concurrent creation conflict
            new() { Key = "Lis:LisChannels:10:Name", Value = "ExistingChannel10" }
        };

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(existingSettings);

        var newEntryPage = new Page
        {
            Key = "Lis:LisChannels:10",
            Children =
            [
                new Option
                {
                    Key = "Lis:LisChannels:10:Name",
                    Type = OptionType.String,
                    MaxLength = 100,
                    DefaultValue = string.Empty,
                    Value = "Channel10"
                }
            ]
        };

        _mockValidator
            .Setup(v => v.ValidateCanAddAsync("Lis:LisChannels", "Lis", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PageListMaxItemsValidationResult.Success());

        var request = new UpdateSettingCommand(string.Empty, newEntryPage, false);

        // Act
        var result = await _sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Error == SettingUpdateError.ConcurrentModification);

        _mockValidator.Verify(
            v => v.ValidateCanAddAsync("Lis:LisChannels", "Lis", It.IsAny<CancellationToken>()),
            Times.Once);
        _mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static string GetDeletedSettingMessage() =>
        Translations.ResourceManager.GetString("DeletedSetting", Translations.Culture) ??
        "The setting no longer exists.";
    #endregion
}

[RootConfigSection(sortOrder: 30)]
[DisplayNameLocal("ProviderManagedSave")]
internal class ProviderManagedSaveSettings
{
    [DisplayNameLocal("Entries")]
    [ProviderPageList(nameof(UpdateCommandPageListEntryProvider), nameof(ProviderManagedSaveEntry.EntryKey))]
    public IEnumerable<ProviderManagedSaveEntry> Entries { get; set; } = [];
}

internal class ProviderManagedSaveEntry
{
    public string EntryKey { get; set; } = string.Empty;

    [ReadOnlySetting]
    [DisplayNameLocal("Name")]
    public string Name { get; set; } = string.Empty;

    [DisplayNameLocal("Enabled")]
    public bool Enabled { get; set; }
}

[RootConfigSection(sortOrder: 31)]
[DisplayNameLocal("ProviderManagedWithoutNameSave")]
internal class ProviderManagedWithoutNameSaveSettings
{
    [DisplayNameLocal("Entries")]
    [ProviderPageList(nameof(UpdateCommandPageListEntryProvider), nameof(ProviderManagedWithoutNameSaveEntry.EntryKey))]
    public IEnumerable<ProviderManagedWithoutNameSaveEntry> Entries { get; set; } = [];
}

internal class ProviderManagedWithoutNameSaveEntry
{
    public string EntryKey { get; set; } = string.Empty;

    [DisplayNameLocal("Enabled")]
    public bool Enabled { get; set; }
}

[RootConfigSection(sortOrder: 31)]
[DisplayNameLocal("ProviderManagedEditableSave")]
internal class ProviderManagedEditableSaveSettings
{
    [DisplayNameLocal("Entries")]
    [ProviderPageList(nameof(UpdateCommandPageListEntryProvider), nameof(ProviderManagedEditableSaveEntry.EntryKey))]
    public IEnumerable<ProviderManagedEditableSaveEntry> Entries { get; set; } = [];
}

internal class ProviderManagedEditableSaveEntry
{
    public string EntryKey { get; set; } = string.Empty;

    [DisplayNameLocal("Name")]
    public string Name { get; set; } = string.Empty;
}

[RootConfigSection(sortOrder: 20)]
[DisplayNameLocal("RootLabel")]
internal class PageListPage
{
    [DisplayNameLocal("PageList Label")]
    public IEnumerable<BasicPage> Pages { get; set; } = [];
}
internal sealed class UpdateCommandPageListEntryProvider : IPageListEntryProvider
{
    public Task<IReadOnlyList<PageListEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PageListEntry>>([]);
    }
}
