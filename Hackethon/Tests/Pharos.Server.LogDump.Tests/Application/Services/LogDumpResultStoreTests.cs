using Pharos.Server.LogDump.Application.Interfaces;
using Pharos.Server.LogDump.Application.Services;

namespace Pharos.Server.LogDump.Tests.Application.Services;

public sealed class LogDumpResultStoreTests
{
    [Fact]
    public void Result_WhenNotSet_ShouldBeNull()
    {
        // Arrange
        var sut = new LogDumpResultStore();

        // Assert
        sut.Result.Should().BeNull();
    }

    [Fact]
    public void Result_WhenAssigned_ShouldReturnStoredResult()
    {
        // Arrange
        var sut = new LogDumpResultStore();
        var result = new LogDumpResult(
            Guid.NewGuid(),
            "LogDump.zip",
            DateTimeOffset.UtcNow,
            LogDumpRequestSource.Pharos);

        // Act
        sut.Result = result;

        // Assert
        sut.Result.Should().Be(result);
    }

    [Fact]
    public void Result_WhenAssignedAgain_ShouldReplaceStoredResult()
    {
        // Arrange
        var sut = new LogDumpResultStore
        {
            Result = new LogDumpResult(
                Guid.NewGuid(),
                "First.zip",
                DateTimeOffset.UtcNow.AddMinutes(-1),
                LogDumpRequestSource.Pharos)
        };

        var result = new LogDumpResult(
            Guid.NewGuid(),
            "Second.zip",
            DateTimeOffset.UtcNow,
            LogDumpRequestSource.LabCommunity);

        // Act
        sut.Result = result;

        // Assert
        sut.Result.Should().Be(result);
    }
}
