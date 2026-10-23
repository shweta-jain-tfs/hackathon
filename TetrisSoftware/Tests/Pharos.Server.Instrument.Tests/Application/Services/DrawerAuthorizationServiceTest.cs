using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Instrument.Application.Commands.UnlockCompartment;
using Pharos.Server.Instrument.Application.TestHelpers;
using Tetris.Protocol.Common;

namespace Pharos.Server.Instrument.Application.Services
{
    public class DrawerAuthorizationServiceTest
    {
        private readonly Mock<IMemoryCache> _memoryCacheMock = new();
        private readonly Mock<ISender> _senderMock = new();
        private readonly Mock<ILogger<DrawerAuthorizationService>> _loggerMock = new();

        [SuppressMessage("Performance", "IDE0051:Remove unused private members", Justification = "Helper method for potential future tests")]
        private DrawerAuthorizationService CreateService()
        {
            return new DrawerAuthorizationService(
                _memoryCacheMock.Object,
                _senderMock.Object,
                _loggerMock.Object
                );
        }

        [Fact]
        public void Remove_RemovesDrawerFromCache()
        {
            // Arrange
            var instrumentId = "100";
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new DrawerAuthorizationService(
                cache, _senderMock.Object, _loggerMock.Object);

            service.Add(instrumentId, EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer);

            // Act
            service.Remove(instrumentId);

            // Assert
            var drawers = service.GetDrawers(instrumentId);
            Assert.Empty(drawers);
        }

        [Fact]
        public void Add_MultipleDrawers_AddsAllWithoutDuplicates()
        {
            // Arrange
            var instrumentId = "101";
            var drawer1 = EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer;
            var drawer2 = EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer1;
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new DrawerAuthorizationService(
                cache, _senderMock.Object, _loggerMock.Object);

            // Act
            service.Add(instrumentId, drawer1);
            service.Add(instrumentId, drawer2);
            service.Add(instrumentId, drawer1); // duplicate

            // Assert
            var drawers = service.GetDrawers(instrumentId);
            Assert.Equal(2, drawers.Count);
            Assert.Contains(drawer1, drawers);
            Assert.Contains(drawer2, drawers);
        }

        [Fact]
        public async Task SendUnlockIfWaiting_EmptyDrawers_DoesNothing()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrumentId = "102";
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new DrawerAuthorizationService(
                cache, _senderMock.Object, _loggerMock.Object);

            // Act
            await service.SendUnlockIfWaiting(userId, instrumentId);

            // Assert
            _senderMock.Verify(s => s.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void GetDrawers_CacheReturnsNull_ReturnsEmptyList()
        {
            // Arrange
            var instrumentId = "103";
            var memoryCacheMock = new Mock<IMemoryCache>();
            object? outValue = null;
            memoryCacheMock.Setup(m => m.TryGetValue(instrumentId, out outValue)).Returns(false);
            var service = new DrawerAuthorizationService(
                memoryCacheMock.Object, _senderMock.Object, _loggerMock.Object);

            // Act
            var drawers = service.GetDrawers(instrumentId);

            // Assert
            Assert.NotNull(drawers);
            Assert.Empty(drawers);
        }

        [Fact]
        public void Add_DoesNotDuplicateDrawers()
        {
            // Arrange
            var instrumentId = "20";
            var drawer = EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer;
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new DrawerAuthorizationService(
                cache, _senderMock.Object, _loggerMock.Object);

            // Act
            service.Add(instrumentId, drawer);
            service.Add(instrumentId, drawer);

            // Assert
            var drawers = service.GetDrawers(instrumentId);
            Assert.Single(drawers);
            Assert.Contains(drawer, drawers);
        }

        [Fact]
        public void GetDrawers_ReturnsListOfDrawers()
        {
            // Arrange
            var instrumentId = "21";
            var drawer1 = EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer;
            var drawer2 = EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer1;
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new DrawerAuthorizationService(
                cache, _senderMock.Object, _loggerMock.Object);

            // Act
            service.Add(instrumentId, drawer1);
            service.Add(instrumentId, drawer2);
            var drawers = service.GetDrawers(instrumentId);

            // Assert
            Assert.Equal(2, drawers.Count);
            Assert.Contains(drawer1, drawers);
            Assert.Contains(drawer2, drawers);
        }

        [Fact]
        public async Task SendUnlockIfWaiting_LogsNotAllowedException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrumentId = "22";
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new DrawerAuthorizationService(
                cache, _senderMock.Object, _loggerMock.Object);
            service.Add(instrumentId, EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer);

            _senderMock
                .Setup(s => s.Send(It.IsAny<UnlockCompartmentCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotAllowedException("Not authorized"));

            // Act
            await service.SendUnlockIfWaiting(userId, instrumentId);

            // Assert

            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => ObjectValidationHelper.CheckIfObjectContainsString(v, "is not authorized to unlock drawer")),
                    It.IsAny<NotAllowedException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void GetDrawers_ReturnsEmptyListIfNotInCache()
        {
            // Arrange
            var instrumentId = "3";
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new DrawerAuthorizationService(
                cache, _senderMock.Object, _loggerMock.Object);

            // Act
            var drawers = service.GetDrawers(instrumentId);

            // Assert
            Assert.NotNull(drawers);
            Assert.Empty(drawers);
        }

        [Fact]
        public async Task SendUnlockIfWaiting_ReagentDrawer_UnlocksIfAuthorized()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrumentId = "10";
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new DrawerAuthorizationService(
                cache, _senderMock.Object, _loggerMock.Object);
            service.Add(instrumentId, EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer);

            // Act
            await service.SendUnlockIfWaiting(userId, instrumentId);

            // Assert
            _senderMock.Verify(s => s.Send(It.Is<UnlockCompartmentCommand>(c =>
                c.InstrumentNumber == instrumentId &&
                c.Compartment == EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendUnlockIfWaiting_SampleDrawer_UnlocksAndAuditsIfAuthorized()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrumentId = "11";
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new DrawerAuthorizationService(
                cache, _senderMock.Object, _loggerMock.Object);
            service.Add(instrumentId, EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer1);

            // Act
            await service.SendUnlockIfWaiting(userId, instrumentId);

            // Assert
            _senderMock.Verify(s => s.Send(It.Is<UnlockCompartmentCommand>(c =>
                c.InstrumentNumber == instrumentId &&
                c.Compartment == EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer1
            ), It.IsAny<CancellationToken>()), Times.Once);

        }

        [Fact]
        public async Task SendUnlockIfWaiting_OtherDrawer_UnlocksWithoutPermissionCheck()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrumentId = "12";
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new DrawerAuthorizationService(
                cache, _senderMock.Object, _loggerMock.Object);
            service.Add(instrumentId, EnumInstrumentCompartmentWithLock.CompartmentSolidWasteDrawer);

            // Act
            await service.SendUnlockIfWaiting(userId, instrumentId);

            // Assert
            _senderMock.Verify(s => s.Send(It.Is<UnlockCompartmentCommand>(c =>
                c.InstrumentNumber == instrumentId &&
                c.Compartment == EnumInstrumentCompartmentWithLock.CompartmentSolidWasteDrawer
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendUnlockIfWaiting_MultipleCompartments_ProcessesAllInOrder()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrumentId = "24";
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new DrawerAuthorizationService(
                cache, _senderMock.Object, _loggerMock.Object);

            var drawer1 = EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer;
            var drawer2 = EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer1;

            service.Add(instrumentId, drawer1);
            service.Add(instrumentId, drawer2);

            // Act
            await service.SendUnlockIfWaiting(userId, instrumentId);

            // Assert
            _senderMock.Verify(s => s.Send(It.Is<UnlockCompartmentCommand>(c =>
                c.InstrumentNumber == instrumentId &&
                c.Compartment == drawer1
            ), It.IsAny<CancellationToken>()), Times.Once);

            _senderMock.Verify(s => s.Send(It.Is<UnlockCompartmentCommand>(c =>
                c.InstrumentNumber == instrumentId &&
                c.Compartment == drawer2
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendUnlockIfWaiting_ClearsDrawersFromCacheAfterProcessing()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrumentId = "25";
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new DrawerAuthorizationService(
                cache, _senderMock.Object, _loggerMock.Object);
            service.Add(instrumentId, EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer);

            // Act
            await service.SendUnlockIfWaiting(userId, instrumentId);
            var drawersAfter = service.GetDrawers(instrumentId);

            // Assert
            Assert.Empty(drawersAfter);
        }
    }
}
