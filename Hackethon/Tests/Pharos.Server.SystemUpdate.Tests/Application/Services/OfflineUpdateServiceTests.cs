using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Process;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Application.Services;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Tests.Application.Services;
public class OfflineUpdateServiceTests
{
    internal readonly Mock<IFileSystem> _fileSystem = new();
    internal readonly Mock<IDateTime> _dateTime = new();
    internal readonly Mock<IUpdateMediumStateStore> _updateMedium = new();
    internal readonly Mock<IUpdatePackageAccessor> _packageAccessor = new();
    internal readonly Mock<IProcessRunner> _processRunner = new();
    internal readonly Mock<ISystemUpdateDbContext> _dbContext = new();
    internal readonly Mock<IPharosVersionService> _versionService = new();
    internal readonly OfflineUpdateService _sut;
    private readonly string _driveName;

    public OfflineUpdateServiceTests()
    {
        _driveName = "E:";
        var mockState = new UpdateMediumState(_driveName);
        _updateMedium.Setup(m => m.Get()).Returns(mockState);

        _sut = new OfflineUpdateService(_fileSystem.Object,
            new Mock<ILogger<OfflineUpdateService>>().Object,
            _dateTime.Object,
            _processRunner.Object,
            _packageAccessor.Object,
            _dbContext.Object,
            _versionService.Object,
            _updateMedium.Object);
    }

    public class TheGetAvailableUpdatesMethod : OfflineUpdateServiceTests
    {
        [Fact]
        public void ThrowsException_WhenDriveIsEmpty()
        {
            // Arrange
            _updateMedium.Setup(m => m.Get()).Returns(new UpdateMediumState(string.Empty));

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _sut.GetAvailableUpdates(TestContext.Current.CancellationToken));
        }

        [Fact]
        public void EnumeratesUpdatePackageContents()
        {
            // Arrange
            _packageAccessor.Setup(pa => pa.EnumerateContents(_driveName, It.IsAny<CancellationToken>()))
                            .Returns(new List<string> { "Update1.msi", "Update2.msu" });

            // Act
            var updates = _sut.GetAvailableUpdates(TestContext.Current.CancellationToken);

            // Assert
            _packageAccessor.Verify(pa => pa.EnumerateContents(_driveName, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(2, updates.Count);
        }
    }

    public class TheInitiateUpdateMethod : OfflineUpdateServiceTests
    {
        private readonly string _stagedPath;
        private readonly string _updatePath;

        public TheInitiateUpdateMethod()
        {
            _fileSystem.Setup(fs => fs.File).Returns(new Mock<IFile>().Object);

            _fileSystem.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
            _processRunner.Setup(m => m.Start(It.IsAny<string>(), It.IsAny<string>())).Returns(new Mock<IStartedProcess>().Object);

            _stagedPath = Path.Combine(Path.GetTempPath(), "Pharos", "SystemUpdate");
            const string updateFile = "PharosUpdate.msi";
            _updatePath = Path.Combine(_stagedPath, updateFile);

            _packageAccessor.Setup(m => m.EnumerateContents(_driveName, It.IsAny<CancellationToken>())).Returns(new List<string>
            {
                updateFile
            });
            _packageAccessor.Setup(m => m.StageToDirectory(_driveName, It.IsAny<CancellationToken>())).Returns(_stagedPath);
            _packageAccessor.Setup(m => m.FindUpdateCandidates(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(new List<string>
            {
                _updatePath
            });

            _dbContext.Setup(m => m.SystemUpdateSessions).ReturnsDbSet(new List<SystemUpdateSession>());
        }

        [Fact]
        public async Task ThrowsException_WhenDriveIsEmpty()
        {
            // Arrange
            _updateMedium.Setup(m => m.Get()).Returns(new UpdateMediumState(string.Empty));

            // Act
            Task Result() => _sut.InitiateUpdate();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(Result);
        }

        [Fact]
        public async Task EnumeratesUpdatePackageContents()
        {
            // Arrange
            _packageAccessor.Setup(m => m.EnumerateContents(_driveName, It.IsAny<CancellationToken>())).Returns(new List<string>());

            // Act
            await _sut.InitiateUpdate(TestContext.Current.CancellationToken);

            // Assert
            _packageAccessor.Verify(pa => pa.EnumerateContents(_driveName, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ThrowsOperationCanceled_WhenTokenCanceledEarly()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            _packageAccessor.Setup(fs => fs.EnumerateContents(_driveName, cts.Token))
                .Returns(new List<string>
                {
                    "PharosUpdate.msi"
                }).Callback(cts.Cancel);

            // Act
            Task Result() => _sut.InitiateUpdate(cts.Token);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(Result);
        }

        [Fact]
        public async Task StagesPackageContentToDirectory()
        {
            // Act
            await _sut.InitiateUpdate(TestContext.Current.CancellationToken);

            // Assert
            _packageAccessor.Verify(pa => pa.StageToDirectory(_driveName, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FindsUpdateCandidatesInStagedDirectory()
        {
            // Act
            await _sut.InitiateUpdate(TestContext.Current.CancellationToken);

            // Assert
            _packageAccessor.Verify(pa => pa.FindUpdateCandidates(_stagedPath, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunsInstallerProcessQuietly_WhenMsiCandidateFound()
        {
            // Act
            await _sut.InitiateUpdate(TestContext.Current.CancellationToken);

            // Assert
            _processRunner.Verify(pr => pr.Start("msiexec.exe", It.Is<string>(args => args.Contains(_updatePath))), Times.Once);
            _processRunner.Verify(pr => pr.Start("msiexec.exe", It.Is<string>(args => args.Contains("/quiet"))), Times.Once);
        }

        [Fact]
        public async Task CreatesInstallerLogFolder_WhenFolderDoesNotExist()
        {
            // Arrange
            var logFolderPath = @"C:\ProgramData\Thermo Fisher Scientific\Pharos\InstallationLogs";
            _fileSystem.Setup(fs => fs.Directory.Exists(logFolderPath)).Returns(false);

            // Act
            await _sut.InitiateUpdate(TestContext.Current.CancellationToken);

            // Assert
            _fileSystem.Verify(fs => fs.Directory.CreateDirectory(logFolderPath), Times.Once);
        }

        [Fact]
        public async Task ThrowsUpdateFailure_WhenErrorCodeReturnedFromInstallerProcess()
        {
            // Arrange
            var startedProcess = new Mock<IStartedProcess>();
            startedProcess.Setup(sp => sp.WaitForExitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            startedProcess.Setup(sp => sp.ExitCode).Returns(1); // Non-zero exit code indicates failure

            _processRunner.Setup(pr => pr.Start("msiexec.exe", It.IsAny<string>()))
                .Returns(startedProcess.Object);

            // Act
            Task Result() => _sut.InitiateUpdate();

            // Assert
            await Assert.ThrowsAsync<UpdateFailedException>(Result);
        }

        [Fact]
        public async Task ThrowsUpdateFailure_WhenInstallerProcessFailsToStart()
        {
            // Arrange
            _processRunner.Setup(pr => pr.Start("msiexec.exe", It.IsAny<string>()))
                .Returns((IStartedProcess?)null); // Simulate failure to start process

            // Act
            Task Result() => _sut.InitiateUpdate();

            // Assert
            await Assert.ThrowsAsync<UpdateFailedException>(Result);
        }

        [Fact]
        public async Task ThrowsUpdateFailure_WhenInstallerNotFound()
        {
            // Arrange
            _packageAccessor.Setup(m => m.FindUpdateCandidates(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(new List<string>()); // No installer found

            // Act
            Task Result() => _sut.InitiateUpdate();

            // Assert
            await Assert.ThrowsAsync<UpdateFailedException>(Result);
        }

        [Fact]
        public async Task SavesPendingUpdateToDatabase()
        {
            // Arrange
            var mockDateTime = new DateTime(2024, 1, 1, 12, 0, 0);
            _dateTime.Setup(dt => dt.UtcNow).Returns(mockDateTime);

            var currentVersion = "1.0.0";
            _versionService.Setup(m => m.Version).Returns(currentVersion);

            // Act
            await _sut.InitiateUpdate(TestContext.Current.CancellationToken);

            // Assert
            _dbContext.Verify(m => m.SystemUpdateSessions.AddAsync(It.Is<SystemUpdateSession>(s =>
                s.Source == UpdateSource.Usb &&
                s.UsbDrive == _driveName &&
                s.PreviousVersion == currentVersion &&
                s.StartedAtUtc == mockDateTime &&
                s.Status == UpdateStatus.Pending
            ), It.IsAny<CancellationToken>()), Times.Once);

            _dbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
