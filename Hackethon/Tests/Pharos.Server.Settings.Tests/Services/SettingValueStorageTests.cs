using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Settings.Services;

public class SettingValueStorageTests
{
    [Fact]
    public void CreateSetting_WithEncryptedOption_ShouldStoreInProtectedValue()
    {
        // Arrange
        var option = new Option
        {
            Key = "Test:Token",
            Value = "secret-value",
            IsEncrypted = true
        };

        // Act
        var setting = SettingValueStorage.CreateSetting(option);

        // Assert
        setting.ProtectedValue.Should().Be("secret-value");
        setting.Value.Should().BeNull();
    }

    [Fact]
    public void CreateSetting_WithNonEncryptedOption_ShouldStoreInValue()
    {
        // Arrange
        var option = new Option
        {
            Key = "Test:Name",
            Value = "normal-value",
            IsEncrypted = false
        };

        // Act
        var setting = SettingValueStorage.CreateSetting(option);

        // Assert
        setting.Value.Should().Be("normal-value");
        setting.ProtectedValue.Should().BeNull();
    }

    [Fact]
    public void SetStoredValue_WithEncryptedOption_ShouldStoreInProtectedValue()
    {
        // Arrange
        var setting = new Setting { Key = "Test:Token" };
        var option = new Option { Key = "Test:Token", IsEncrypted = true };

        // Act
        SettingValueStorage.SetStoredValue(setting, option, "encrypted-secret");

        // Assert
        setting.ProtectedValue.Should().Be("encrypted-secret");
        setting.Value.Should().BeNull();
    }

    [Fact]
    public void SetStoredValue_WithNonEncryptedOption_ShouldStoreInValue()
    {
        // Arrange
        var setting = new Setting { Key = "Test:Name" };
        var option = new Option { Key = "Test:Name", IsEncrypted = false };

        // Act
        SettingValueStorage.SetStoredValue(setting, option, "plain-value");

        // Assert
        setting.Value.Should().Be("plain-value");
        setting.ProtectedValue.Should().BeNull();
    }

    [Fact]
    public void GetEffectiveValue_WithEncryptedOption_ShouldReadFromProtectedValue()
    {
        // Arrange
        var setting = new Setting
        {
            Key = "Test:Token",
            ProtectedValue = "protected-secret",
            Value = null
        };
        var option = new Option { Key = "Test:Token", IsEncrypted = true };

        // Act
        var result = SettingValueStorage.GetEffectiveValue(setting, option);

        // Assert
        result.Should().Be("protected-secret");
    }

    [Fact]
    public void GetEffectiveValue_WithNonEncryptedOption_ShouldReadFromValue()
    {
        // Arrange
        var setting = new Setting
        {
            Key = "Test:Name",
            Value = "normal-value",
            ProtectedValue = null
        };
        var option = new Option { Key = "Test:Name", IsEncrypted = false };

        // Act
        var result = SettingValueStorage.GetEffectiveValue(setting, option);

        // Assert
        result.Should().Be("normal-value");
    }

    [Fact]
    public void NeedsStorageMigration_EncryptedOptionWithValueInOldColumn_ShouldReturnTrue()
    {
        // Arrange - encrypted option but value still in Value column (needs migration)
        var setting = new Setting
        {
            Key = "Test:Token",
            Value = "old-location",
            ProtectedValue = null
        };
        var option = new Option { Key = "Test:Token", IsEncrypted = true };

        // Act
        var result = SettingValueStorage.NeedsStorageMigration(setting, option);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void NeedsStorageMigration_EncryptedOptionWithValueInProtectedColumn_ShouldReturnFalse()
    {
        // Arrange - encrypted option with value correctly in ProtectedValue
        var setting = new Setting
        {
            Key = "Test:Token",
            Value = null,
            ProtectedValue = "correct-location"
        };
        var option = new Option { Key = "Test:Token", IsEncrypted = true };

        // Act
        var result = SettingValueStorage.NeedsStorageMigration(setting, option);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CreateSetting_WithKeyAndValue_EncryptedOption_ShouldStoreInProtectedValue()
    {
        // Arrange
        var options = new Dictionary<string, Option>
        {
            ["Test:Token"] = new Option { Key = "Test:Token", IsEncrypted = true }
        };

        // Act
        var setting = SettingValueStorage.CreateSetting("Test:Token", "secret", options);

        // Assert
        setting.ProtectedValue.Should().Be("secret");
        setting.Value.Should().BeNull();
    }

    [Fact]
    public void ToSettingsDictionary_EncryptedSetting_ShouldReadFromProtectedValue()
    {
        // Arrange
        var settings = new List<Setting>
        {
            new() { Key = "Test:Token", ProtectedValue = "protected-secret", Value = null },
            new() { Key = "Test:Name", Value = "normal", ProtectedValue = null }
        };
        var options = new Dictionary<string, Option>
        {
            ["Test:Token"] = new Option { Key = "Test:Token", IsEncrypted = true },
            ["Test:Name"] = new Option { Key = "Test:Name", IsEncrypted = false }
        };

        // Act
        var result = SettingValueStorage.ToSettingsDictionary(settings, options);

        // Assert
        result["Test:Token"].Should().Be("protected-secret");
        result["Test:Name"].Should().Be("normal");
    }
}
