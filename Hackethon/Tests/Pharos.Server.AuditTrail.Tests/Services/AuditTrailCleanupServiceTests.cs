using Moq.EntityFrameworkCore;
using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.AuditTrail.Application.Interfaces;
using Pharos.Server.AuditTrail.Domain;

namespace Pharos.Server.AuditTrail.Services;

public class AuditTrailCleanupServiceTests
{
    private readonly Mock<IAuditTrailDbContext> _dbContextMock;
    private readonly Mock<AuditDataProtectionService> _dataProtectionMock;
    private readonly AuditTrailCleanupService _service;

    public AuditTrailCleanupServiceTests()
    {
        _dbContextMock = new Mock<IAuditTrailDbContext>();
        _dataProtectionMock = new Mock<AuditDataProtectionService>();
        _service = new AuditTrailCleanupService(_dbContextMock.Object, _dataProtectionMock.Object);

        // common setup for all tests
        _dbContextMock.Setup(c => c.HashesBulkUpdateAsync(It.IsAny<List<(int, string)>>()))
            .Returns(Task.CompletedTask);
    }
    
    [Fact]
    public async Task Cleanup_WhenNoExpiredRecords_ReturnsFalse()
    {
        // Arrange
        _dbContextMock.Setup(c => c.DeleteExpiredRecordsAsync(It.IsAny<DateTimeOffset>(), 5000, TestContext.Current.CancellationToken))
            .ReturnsAsync(0);

        // Act
        var result = await _service.AuditTrailCleanupAsync();

        // Assert
        Assert.False(result);
        _dataProtectionMock.Verify(dp => dp.ComputeHash(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _dataProtectionMock.Verify(dp => dp.SaveHash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Cleanup_ExpiredRecordsExist_DeletesRecordsAndReturnsTrue()
    {
        // Arrange
        var deleteCalls = 0;
        _dbContextMock.Setup(c => c.DeleteExpiredRecordsAsync(It.IsAny<DateTimeOffset>(), 5000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => deleteCalls++ == 0 ? 2 : 0);
        _dbContextMock.Setup(c => c.AuditTrailRecords).ReturnsDbSet(new List<AuditTrailRecord>());

        // Act
        var result = await _service.AuditTrailCleanupAsync();

        // Assert
        Assert.True(result);
        _dbContextMock.Verify(c => c.DeleteExpiredRecordsAsync(It.IsAny<DateTimeOffset>(), 5000, It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }


    [Fact]
    public async Task Cleanup_RecomputesHashesAfterDeletion()
    {
        // Arrange
        var method = typeof(AuditTrailCleanupService)
            .GetMethod("RecomputeAuditTrailHashes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var records = new List<AuditTrailRecord>
        {
            new()
                { Id = 1, Created = DateTimeOffset.Now.AddYears(-6), EventId = EventId.UserLoggedIntoPharos, EventData = [] },
            new()
                { Id = 2, Created = DateTimeOffset.Now.AddYears(-6), EventId = EventId.UserLoggedOutPharos, EventData = [] }
        };
        
        _dbContextMock.Setup(c => c.AuditTrailRecords).ReturnsDbSet(records);

        _dbContextMock.Setup(c => c.HashesBulkUpdateAsync(It.IsAny<List<(int, string)>>()))
            .Returns(Task.CompletedTask);

        _dataProtectionMock.Setup(dp => dp.ComputeHash(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string prev, string data) => prev + "-" + data);
        _dataProtectionMock.Setup(dp => dp.SaveHash(It.IsAny<string>()));

        // Act
        await (Task)method!.Invoke(_service, null)!;

        // Assert
        _dataProtectionMock.Verify(dp => dp.ComputeHash(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        _dbContextMock.Verify(c => c.HashesBulkUpdateAsync(It.IsAny<List<(int, string)>>()), Times.AtLeastOnce);
        _dataProtectionMock.Verify(dp => dp.SaveHash(It.IsAny<string>()), Times.Once);
    }
}
