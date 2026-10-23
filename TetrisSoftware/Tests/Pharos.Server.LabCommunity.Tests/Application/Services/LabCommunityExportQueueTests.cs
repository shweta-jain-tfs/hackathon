using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Pharos.Server.Infrastructure;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Services;
using Pharos.Server.LabCommunity.Domain;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Services
{
    public static class LabCommunityExportQueueTests
    {
        public class TheEnqueueAsyncMethod
        {
            private readonly Mock<ILabCommunityDbContext> _dbContextMock;
            private readonly Mock<DbSet<ExportItem>> _exportItemsMock;
            private readonly Mock<IDateTime> _dateTimeMock;
            private readonly LabCommunityExportQueue _sut;

            public TheEnqueueAsyncMethod()
            {
                _dbContextMock = new Mock<ILabCommunityDbContext>();
                _exportItemsMock = new Mock<DbSet<ExportItem>>();
                _dateTimeMock = new Mock<IDateTime>();

                _dbContextMock.Setup(db => db.ExportItems).Returns(_exportItemsMock.Object);

                _sut = new LabCommunityExportQueue(
                    _dbContextMock.Object,
                    _dateTimeMock.Object,
                    Mock.Of<ILogger<LabCommunityExportQueue>>());
            }

            [Trait("ProductRequirement", "PR_2188")]
            [Trait("ModuleRequirement", "MR_5998")]
            [Fact]
            public async Task StoresPendingExportItem_WhenDeduplicationKeyDoesNotExist()
            {
                // Arrange
                var now = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero);
                _dateTimeMock.Setup(t => t.UtcNow).Returns(now);
                _dbContextMock.Setup(db => db.GetExportItemByDeduplicationKeyAsync(
                        "InstrumentError:SN123:4b0f3d55-1790-4c98-9a5c-21b8edff101a",
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ExportItem)null!);

                // Act
                await _sut.EnqueueAsync(
                    ExportItemType.InstrumentError,
                    "InstrumentError:SN123:4b0f3d55-1790-4c98-9a5c-21b8edff101a",
                    "{\"errorId\":\"4b0f3d55-1790-4c98-9a5c-21b8edff101a\"}",
                    1,
                    TestContext.Current.CancellationToken);

                // Assert
                _exportItemsMock.Verify(items => items.Add(It.Is<ExportItem>(item =>
                    item.ExportItemType == ExportItemType.InstrumentError &&
                    item.DeduplicationKey == "InstrumentError:SN123:4b0f3d55-1790-4c98-9a5c-21b8edff101a" &&
                    item.PayloadJson == "{\"errorId\":\"4b0f3d55-1790-4c98-9a5c-21b8edff101a\"}" &&
                    item.PayloadVersion == 1 &&
                    item.Status == ExportItemStatus.Pending &&
                    item.AttemptCount == 0 &&
                    item.CreatedAtUtc == now &&
                    item.NextAttemptAtUtc == null &&
                    item.LastAttemptAtUtc == null &&
                    item.LastError == null)), Times.Once);

                _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_2188")]
            [Trait("ModuleRequirement", "MR_5998")]
            [Fact]
            public async Task SkipsExportItem_WhenDeduplicationKeyAlreadyExists()
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetExportItemByDeduplicationKeyAsync(
                        "InstrumentError:SN123:4b0f3d55-1790-4c98-9a5c-21b8edff101a",
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new ExportItem());

                // Act
                await _sut.EnqueueAsync(
                    ExportItemType.InstrumentError,
                    "InstrumentError:SN123:4b0f3d55-1790-4c98-9a5c-21b8edff101a",
                    "{\"errorId\":\"4b0f3d55-1790-4c98-9a5c-21b8edff101a\"}",
                    1,
                    TestContext.Current.CancellationToken);

                // Assert
                _exportItemsMock.Verify(items => items.Add(It.IsAny<ExportItem>()), Times.Never);
                _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        public class ThePeekBatchAsyncMethod
        {
            [Trait("ProductRequirement", "PR_2188")]
            [Trait("ModuleRequirement", "MR_5998")]
            [Fact]
            public async Task ReturnsReadyItemsForExportTypeInQueueOrder()
            {
                var now = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero);
                var expected = new ExportItem
                {
                    ExportItemType = ExportItemType.InstrumentError,
                    Status = ExportItemStatus.Failed,
                    CreatedAtUtc = now.AddHours(-2),
                    NextAttemptAtUtc = now
                };
                var exportItems = new[]
                {
                    new ExportItem
                    {
                        ExportItemType = ExportItemType.CurveControlResult,
                        Status = ExportItemStatus.Pending,
                        CreatedAtUtc = now.AddHours(-3)
                    },
                    new ExportItem
                    {
                        ExportItemType = ExportItemType.InstrumentError,
                        Status = ExportItemStatus.Pending,
                        CreatedAtUtc = now.AddHours(-1)
                    },
                    new ExportItem
                    {
                        ExportItemType = ExportItemType.InstrumentError,
                        Status = ExportItemStatus.Failed,
                        CreatedAtUtc = now.AddHours(-4),
                        NextAttemptAtUtc = now.AddMinutes(1)
                    },
                    expected
                }.BuildMockDbSet();
                var dbContextMock = new Mock<ILabCommunityDbContext>();
                dbContextMock.Setup(db => db.ExportItems).Returns(exportItems.Object);
                var dateTimeMock = new Mock<IDateTime>();
                dateTimeMock.Setup(time => time.UtcNow).Returns(now);
                var sut = new LabCommunityExportQueue(
                    dbContextMock.Object,
                    dateTimeMock.Object,
                    Mock.Of<ILogger<LabCommunityExportQueue>>());

                var result = await sut.PeekBatchAsync(
                    ExportItemType.InstrumentError,
                    TestContext.Current.CancellationToken);

                Assert.Equal(2, result.Count);
                Assert.Same(expected, result[0]);
            }
        }

        public class TheDequeueBatchAsyncMethod
        {
            [Trait("ProductRequirement", "PR_2188")]
            [Trait("ModuleRequirement", "MR_5998")]
            [Fact]
            public async Task RemovesExportItems()
            {
                var item = new ExportItem();
                var exportItems = new[] { item }.BuildMockDbSet();
                var dbContextMock = new Mock<ILabCommunityDbContext>();
                dbContextMock.Setup(db => db.ExportItems).Returns(exportItems.Object);
                var sut = new LabCommunityExportQueue(
                    dbContextMock.Object,
                    Mock.Of<IDateTime>(),
                    Mock.Of<ILogger<LabCommunityExportQueue>>());

                await sut.DequeueBatchAsync([item], TestContext.Current.CancellationToken);

                exportItems.Verify(
                    items => items.RemoveRange(It.Is<IEnumerable<ExportItem>>(
                        removedItems => removedItems.Single() == item)),
                    Times.Once);
                dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            }
        }
    }
}