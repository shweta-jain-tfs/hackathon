using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.Infrastructure.Tests.Services;

public class LogFilePathProviderTests
{
    private const string _fileSinkNameKey = "Serilog:WriteTo:1:Args:configure:0:Args:configureLogger:WriteTo:0:Name";
    private const string _fileSinkPathKey = "Serilog:WriteTo:1:Args:configure:0:Args:configureLogger:WriteTo:0:Args:path";

    [Fact]
    public void GetLogFilePath_WhenNestedFileSinkHasPath_ShouldReturnConfiguredPath()
    {
        // Arrange
        var filePath = Path.Combine(Path.GetTempPath(), "pharos-log-.txt");
        var sut = new LogFilePathProvider(CreateConfiguration(filePath));

        // Act
        var result = sut.GetLogFilePath();

        // Assert
        result.Should().Be(filePath);
    }

    [Fact]
    public void GetLogFilePath_WhenPathContainsEnvironmentVariable_ShouldReturnExpandedPath()
    {
        // Arrange
        const string configuredPath = @"%TEMP%\pharos-log-.txt";
        var sut = new LogFilePathProvider(CreateConfiguration(configuredPath));

        // Act
        var result = sut.GetLogFilePath();

        // Assert
        result.Should().Be(Environment.ExpandEnvironmentVariables(configuredPath));
    }

    [Fact]
    public void GetLogFilePath_WhenFileSinkIsMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = new LogFilePathProvider(CreateConfiguration(fileSinkName: "Console"));

        // Act
        var action = sut.GetLogFilePath;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Serilog File sink configuration is missing the path.");
    }

    [Fact]
    public void GetLogFilePath_WhenFileSinkPathIsMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = new LogFilePathProvider(CreateConfiguration(filePath: null));

        // Act
        var action = sut.GetLogFilePath;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Serilog File sink configuration is missing the path.");
    }

    private static IConfiguration CreateConfiguration(string? filePath = "pharos-log-.txt", string fileSinkName = "File")
    {
        var configuration = new Dictionary<string, string?>
        {
            [_fileSinkNameKey] = fileSinkName
        };

        if (filePath != null)
        {
            configuration[_fileSinkPathKey] = filePath;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(configuration).Build();
    }
}
