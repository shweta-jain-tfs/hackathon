using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Application.Services;

namespace Pharos.Server.Lis.Tests;

public class LisSettingTests
{
    private readonly Mock<IOptionsMonitor<LisSettings>> _mockOptionsMonitor;
    private readonly Mock<ILisConnectionFactory> _mockConnectionFactory;
    private readonly ILogger<LisConnectionPool> _logger;
    private readonly Mock<ILisConnection> _mockConnection;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;

    public LisSettingTests()
    {
        _mockOptionsMonitor = new Mock<IOptionsMonitor<LisSettings>>();
        _mockConnectionFactory = new Mock<ILisConnectionFactory>();
        _logger = NullLogger<LisConnectionPool>.Instance;
        _mockConnection = new Mock<ILisConnection>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_565")]
    [Trait("ModuleRequirement", "MR_5382")]
    public async Task WhenLisChannelIsEnabledInSetting_ThenThatParticularLisConnectionShouldBeActive()
    {
        // Arrange
        var enabledChannel = new LisChannel
        {
            Name = "Enabled Test Channel",
            IsEnabled = true,
            ProtocolType = LisProtocolType.Astm
        };

        var lisSettings = new LisSettings
        {
            LisChannels = new List<LisChannel> { enabledChannel }
        };

        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(lisSettings);

        // Setup the connection to be successfully opened
        _mockConnection.Setup(x => x.OpenAsync()).ReturnsAsync(true);
        _mockConnection.Setup(x => x.State).Returns(LisConnectionState.Open);
        _mockConnection.Setup(x => x.Role).Returns(LisConnectionRole.Both);

        _mockConnectionFactory.Setup(x => x.CreateConnection(0, enabledChannel, LisConnectionRole.Import))
                             .Returns(_mockConnection.Object);

        // Act
        using var connectionPool = new LisConnectionPool(
            _mockOptionsMonitor.Object,
            _mockConnectionFactory.Object,
            _mockScopeFactory.Object,
            _logger);

        // Start the background service to initialize connections
        using var cancellationTokenSource = new CancellationTokenSource();
        await connectionPool.StartAsync(cancellationTokenSource.Token);
        await connectionPool.ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);

        // Assert
        var connectionStates = connectionPool.GetConnectionStates(0);
        connectionStates.Should().ContainSingle(s => s.Role == LisConnectionRole.Both
            && s.State == LisConnectionState.Open, "because the channel is enabled and should have an active connection");

        // Verify that the connection factory was called to create a connection
        _mockConnectionFactory.Verify(x => x.CreateConnection(0, enabledChannel, LisConnectionRole.Import), Times.Once);

        // Verify that the connection was opened
        _mockConnection.Verify(x => x.OpenAsync(), Times.Once);

        // Cleanup
        await connectionPool.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_565")]
    [Trait("ModuleRequirement", "MR_5382")]
    public async Task WhenLisChannelIsNotEnabledInSetting_ThenThatParticularLisConnectionShouldNotBeActive()
    {
        // Arrange
        var disabledChannel = new LisChannel
        {
            Name = "Disabled Test Channel",
            IsEnabled = false,
            ProtocolType = LisProtocolType.Astm
        };

        var lisSettings = new LisSettings
        {
            LisChannels = new List<LisChannel> { disabledChannel }
        };

        _mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(lisSettings);

        // Act
        using var connectionPool = new LisConnectionPool(
            _mockOptionsMonitor.Object,
            _mockConnectionFactory.Object,
            _mockScopeFactory.Object,
            _logger);

        // Start the background service to initialize connections
        using var cancellationTokenSource = new CancellationTokenSource();
        await connectionPool.StartAsync(cancellationTokenSource.Token);
        await connectionPool.ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);

        // Assert
        var connectionStates = connectionPool.GetConnectionStates(0);
        connectionStates.Should().ContainSingle(s => s.Role == LisConnectionRole.Both
           && s.State == LisConnectionState.Closed, "because the channel is disabled and should not have an active connection");

        // Verify that the connection factory was NOT called since the channel is disabled
        _mockConnectionFactory.Verify(x => x.CreateConnection(It.IsAny<int>(), It.IsAny<LisChannel>(), It.IsAny<LisConnectionRole>()), Times.Never);

        // Verify that no connection was opened
        _mockConnection.Verify(x => x.OpenAsync(), Times.Never);

        // Cleanup  
        await connectionPool.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_565")]
    [Trait("ModuleRequirement", "MR_5852")]
    public void LisChannelName_WithEmpty_ShouldFailValidation()
    {
        var channel = new LisChannel
        {
            Name = string.Empty,
            ProtocolType = LisProtocolType.Astm
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(channel);
        var isValid = Validator.TryValidateObject(channel, validationContext, validationResults, validateAllProperties: true);

        isValid.Should().BeFalse("because the name is required and cannot be empty");
        validationResults.Should().HaveCountGreaterOrEqualTo(1);
        validationResults.Should().Contain(vr => vr.MemberNames.Contains(nameof(LisChannel.Name)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(49)]
    [Trait("ProductRequirement", "PR_565")]
    [Trait("ModuleRequirement", "MR_5852")]
    public void LisChannelName_WithVariousValidLengths_ShouldPassValidation(int length)
    {
        var channel = new LisChannel
        {
            Name = new string('M', length),
            ProtocolType = LisProtocolType.Astm
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(channel);
        var isValid = Validator.TryValidateObject(channel, validationContext, validationResults, validateAllProperties: true);

        isValid.Should().BeTrue($"because the name with {length} characters is within the maximum length of 50 characters");
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData(51)]
    [InlineData(60)]
    [InlineData(100)]
    [Trait("ProductRequirement", "PR_565")]
    [Trait("ModuleRequirement", "MR_5852")]
    public void LisChannelName_WithVariousInvalidLengths_ShouldFailValidation(int length)
    {
        var channel = new LisChannel
        {
            Name = new string('M', length),
            ProtocolType = LisProtocolType.Astm
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(channel);
        var isValid = Validator.TryValidateObject(channel, validationContext, validationResults, validateAllProperties: true);

        isValid.Should().BeFalse($"because the name with {length} characters exceeds the maximum length of 50 characters");
        validationResults.Should().HaveCountGreaterOrEqualTo(1);
        validationResults.Should().Contain(vr => vr.MemberNames.Contains(nameof(LisChannel.Name)));
    }

    [Trait("ProductRequirement", "PR_2134")]
    [Trait("ModuleRequirement", "MR_5383")]
    [Fact]
    public void DataCapVariant_DefaultValue_ShouldBeOnlineAutomatic()
    {
        var channel = new LisChannel
        {
            Name = "Test Channel",
            ProtocolType = LisProtocolType.DataCap
        };

        channel.DataCapVariant.Should().Be(DataCapVariant.OnlineAutomatic, "because OnlineAutomatic is the default value");
    }

    [Trait("ProductRequirement", "PR_2134")]
    [Trait("ModuleRequirement", "MR_5383")]
    [Theory]
    [InlineData(DataCapVariant.OnlineAutomatic)]
    [InlineData(DataCapVariant.OnlineManual)]
    [InlineData(DataCapVariant.TcpIpFtp)]
    public void DataCapVariant_CanBeSetToAllEnumValues(DataCapVariant variant)
    {
        var channel = new LisChannel
        {
            Name = "Test Channel",
            DataCapVariant = variant
        };

        channel.DataCapVariant = variant;

        channel.DataCapVariant.Should().Be(variant, $"because DataCapVariant should be settable to {variant}");
    }

    [Trait("ProductRequirement", "PR_2134")]
    [Trait("ModuleRequirement", "MR_5383")]
    [Fact]
    public void DataCapVariant_WhenProtocolTypeIsDataCap_ShouldRetainValue()
    {
        var channel = new LisChannel
        {
            Name = "Test Channel",
            ProtocolType = LisProtocolType.DataCap,
            DataCapVariant = DataCapVariant.TcpIpFtp
        };

        var variant = channel.DataCapVariant;

        variant.Should().Be(DataCapVariant.TcpIpFtp, "because the value should be retained when ProtocolType is DataCap");
        channel.DataCapSettings.Should().NotBeNull("because DataCapSettings should be initialized when ProtocolType is DataCap");
    }

    [Trait("ProductRequirement", "PR_2134")]
    [Trait("ModuleRequirement", "MR_5383")]
    [Fact]
    public void DataCapVariant_WithDataCapProtocol_ShouldWorkWithAllVariants()
    {
        var channelAutomatic = new LisChannel
        {
            Name = "Automatic Channel",
            ProtocolType = LisProtocolType.DataCap,
            DataCapVariant = DataCapVariant.OnlineAutomatic
        };

        var channelManual = new LisChannel
        {
            Name = "Manual Channel",
            ProtocolType = LisProtocolType.DataCap,
            DataCapVariant = DataCapVariant.OnlineManual
        };

        var channelFtp = new LisChannel
        {
            Name = "FTP Channel",
            ProtocolType = LisProtocolType.DataCap,
            DataCapVariant = DataCapVariant.TcpIpFtp
        };

        channelAutomatic.DataCapVariant.Should().Be(DataCapVariant.OnlineAutomatic);
        channelAutomatic.DataCapSettings.Should().NotBeNull();

        channelManual.DataCapVariant.Should().Be(DataCapVariant.OnlineManual);
        channelManual.DataCapSettings.Should().NotBeNull();

        channelFtp.DataCapVariant.Should().Be(DataCapVariant.TcpIpFtp);
        channelFtp.DataCapSettings.Should().NotBeNull();
    }

    #region TCP Port Validation Tests

    [Theory]
    [InlineData(0, true)]
    [InlineData(65535, true)]
    [InlineData(-1, false)]
    [InlineData(65536, false)]
    public void TcpPort_Should_Validate_Range_0_To_65535(int port, bool shouldPass)
    {
        // Arrange
        var settings = new AstmCommunicationTcpSettings
        {
            TcpPort = port
        };

        // Act
        var isValid = ValidateSettingsRange(settings, nameof(AstmCommunicationTcpSettings.TcpPort), out var errorMessage);

        // Assert
        if (shouldPass)
        {
            isValid.Should().BeTrue();
            errorMessage.Should().BeNull();
        }
        else
        {
            isValid.Should().BeFalse();
            errorMessage.Should().NotBeNull();
        }
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(65535, true)]
    [InlineData(-1, false)]
    [InlineData(65536, false)]
    [Trait("ModuleRequirement", "MR_5721")]
    [Trait("ProductRequirement", "PR_1921")]
    public void TcpPortForSeparateExportPort_Should_Validate_Range_0_To_65535(int port, bool shouldPass)
    {
        // Arrange
        var settings = new AstmCommunicationTcpSettings
        {
            UseSeparateExportPort = true,
            TcpPortForSeparateExportPort = port
        };

        // Act
        var isValid = ValidateSettingsRange(settings, nameof(AstmCommunicationTcpSettings.TcpPortForSeparateExportPort), out var errorMessage);

        // Assert
        if (shouldPass)
        {
            isValid.Should().BeTrue();
            errorMessage.Should().BeNull();
        }
        else
        {
            isValid.Should().BeFalse();
            errorMessage.Should().NotBeNull();
        }
    }

    /// <summary>
    /// Helper method to validate a property decorated with SettingsRangeAttribute.
    /// </summary>
    private static bool ValidateSettingsRange(object instance, string propertyName, out string? errorMessage)
    {
        errorMessage = null;

        var property = instance.GetType().GetProperty(propertyName);
        if (property == null)
        {
            errorMessage = $"Property {propertyName} not found.";
            return false;
        }

        var settingsRangeAttr = property.GetCustomAttribute<SettingsRangeAttribute>();
        if (settingsRangeAttr == null)
        {
            // No SettingsRange attribute, consider it valid
            return true;
        }

        var value = property.GetValue(instance);
        if (value == null)
        {
            // Null values are considered valid
            return true;
        }

        try
        {
            var comparableValue = Convert.ToDouble(value);
            var comparableMin = Convert.ToDouble(settingsRangeAttr.Min);
            var comparableMax = Convert.ToDouble(settingsRangeAttr.Max);

            if (comparableValue < comparableMin || comparableValue > comparableMax)
            {
                var unit = string.IsNullOrEmpty(settingsRangeAttr.Unit) ? "" : $" {settingsRangeAttr.Unit}";
                errorMessage = $"{propertyName} must be between {settingsRangeAttr.Min}{unit} and {settingsRangeAttr.Max}{unit}.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error validating {propertyName}: {ex.Message}";
            return false;
        }
    }

    #endregion

    #region IP Address Tests

    [Fact]
    public void IpAddress_ShouldHaveDefaultValue_127_0_0_1()
    {
        // Arrange & Act
        var tcpSettings = new AstmCommunicationTcpSettings();

        // Assert
        tcpSettings.IpAddress.Should().Be("127.0.0.1", "because 127.0.0.1 should be the default IP address");
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5721")]
    [Trait("ProductRequirement", "PR_1921")]
    public void IpAddressForSeparateExportPort_ShouldHaveDefaultValue_127_0_0_1()
    {
        // Arrange & Act
        var tcpSettings = new AstmCommunicationTcpSettings();

        // Assert
        tcpSettings.IpAddressForSeparateExportPort.Should().Be("127.0.0.1", "because 127.0.0.1 should be the default IP address for separate export port");
    }

    #endregion

    #region TCP Reconnection Interval Validation Tests

    [Theory]
    [InlineData(0, true)]
    [InlineData(600000, true)]
    [InlineData(-1, false)]
    [InlineData(600001, false)]
    public void TcpReconnectionInterval_Should_Validate_Range_0_To_600000(int interval, bool shouldPass)
    {
        // Arrange
        var settings = new AstmCommunicationTcpSettings
        {
            TcpReconnectionInterval = interval
        };

        // Act
        var isValid = ValidateSettingsRange(settings, nameof(AstmCommunicationTcpSettings.TcpReconnectionInterval), out var errorMessage);

        // Assert
        if (shouldPass)
        {
            isValid.Should().BeTrue();
            errorMessage.Should().BeNull();
        }
        else
        {
            isValid.Should().BeFalse();
            errorMessage.Should().NotBeNull();
            errorMessage.Should().Contain("600000", "because error message should mention the maximum value of 600000 ms");
            errorMessage.Should().Contain("ms", "because error message should include the unit");
        }
    }

    [Fact]
    public void TcpReconnectionInterval_ShouldHaveDefaultValue_10000()
    {
        // Arrange & Act
        var tcpSettings = new AstmCommunicationTcpSettings();

        // Assert
        tcpSettings.TcpReconnectionInterval.Should().Be(10000, "because 10000 ms (10 seconds) should be the default TCP reconnection interval");
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(600000, true)]
    [InlineData(-1, false)]
    [InlineData(600001, false)]
    [Trait("ModuleRequirement", "MR_5721")]
    [Trait("ProductRequirement", "PR_1921")]
    public void TcpReconnectionIntervalForSeparateExportPort_Should_Validate_Range_0_To_600000(int interval, bool shouldPass)
    {
        // Arrange
        var settings = new AstmCommunicationTcpSettings
        {
            UseSeparateExportPort = true,
            TcpReconnectionIntervalForSeparateExportPort = interval
        };

        // Act
        var isValid = ValidateSettingsRange(settings, nameof(AstmCommunicationTcpSettings.TcpReconnectionIntervalForSeparateExportPort), out var errorMessage);

        // Assert
        if (shouldPass)
        {
            isValid.Should().BeTrue();
            errorMessage.Should().BeNull();
        }
        else
        {
            isValid.Should().BeFalse();
            errorMessage.Should().NotBeNull();
            errorMessage.Should().Contain("600000", "because error message should mention the maximum value of 600000 ms");
            errorMessage.Should().Contain("ms", "because error message should include the unit");
        }
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5721")]
    [Trait("ProductRequirement", "PR_1921")]
    public void TcpReconnectionIntervalForSeparateExportPort_ShouldHaveDefaultValue_10000()
    {
        // Arrange & Act
        var tcpSettings = new AstmCommunicationTcpSettings();

        // Assert
        tcpSettings.TcpReconnectionIntervalForSeparateExportPort.Should().Be(10000, "because 10000 ms (10 seconds) should be the default TCP reconnection interval for separate export port");
    }

    #endregion



}
