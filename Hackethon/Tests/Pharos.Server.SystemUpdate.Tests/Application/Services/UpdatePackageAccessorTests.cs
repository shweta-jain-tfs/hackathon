using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.Infrastructure.Compression;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Application.Services;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Tests.Application.Services;

public class UpdatePackageAccessorTests
{
    internal readonly Mock<IZipFile> _zipFile = new();
    internal readonly Mock<IFileSystem> _fileSystem = new();
    internal readonly Mock<IUpdateMediumStateStore> _updateMedium = new();
    internal string _tempFolder = "C:\\TEMP";
    private readonly string _packageDir;
    private readonly string _zipPath;
    private readonly Mock<IZipArchiveEntry> _mockEntry1;
    private readonly Mock<IZipArchiveEntry> _mockEntry2;
    private readonly Mock<IZipArchive> _mockArchive;

    public UpdatePackageAccessorTests()
    {
        var mockState = new UpdateMediumState("E:");
        _updateMedium.Setup(m => m.Get()).Returns(mockState);

        _packageDir = _updateMedium.Object.Get()!.DriveName;

        _fileSystem.Setup(fs => fs.Directory.Exists(_packageDir)).Returns(true);

        _zipPath = Path.Combine(_packageDir, "PharosUpdatePackage.zip");
        _fileSystem.Setup(fs => fs.Directory.EnumerateFiles(_packageDir, "*.zip", SearchOption.AllDirectories))
            .Returns(new List<string> { _zipPath });

        _mockArchive = new Mock<IZipArchive>();

        _mockEntry1 = new Mock<IZipArchiveEntry>();
        _mockEntry1.Setup(e => e.Name).Returns("PharosUpdate.msi");
        _mockEntry1.Setup(e => e.FullName).Returns(_packageDir + "PharosUpdate.msi");

        _mockEntry2 = new Mock<IZipArchiveEntry>();
        _mockEntry2.Setup(e => e.Name).Returns("WindowsUpdate.msu");
        _mockEntry2.Setup(e => e.FullName).Returns(_packageDir + "WindowsUpdate.msu");

        _mockArchive.Setup(a => a.Entries).Returns(new List<IZipArchiveEntry> { _mockEntry1.Object, _mockEntry2.Object});
        _zipFile.Setup(z => z.OpenRead(_zipPath)).Returns(_mockArchive.Object);
    }

    public class TheEnumerateContentsMethod : UpdatePackageAccessorTests
    {
        private readonly UpdatePackageAccessor _sut;

        public TheEnumerateContentsMethod()
        {
            _fileSystem.Setup(fs => fs.Path.GetTempPath()).Returns(_tempFolder);

            var mockState = new UpdateMediumState("E:");
            _updateMedium.Setup(m => m.Get()).Returns(mockState);

            _sut = new UpdatePackageAccessor(_zipFile.Object, _fileSystem.Object, Mock.Of<ILogger<UpdatePackageAccessor>>());
        }

        [Fact]
        public void ReturnsListOfUpdates_WhenDriveHasValidUpdateFiles()
        {
            // Arrange
            var mockEntry3 = new Mock<IZipArchiveEntry>();
            mockEntry3.Setup(e => e.Name).Returns("randomFile.pdf");
            mockEntry3.Setup(e => e.FullName).Returns(_packageDir + "randomFile.pdf");  // Include random file to test filtering

            _mockArchive.Setup(a => a.Entries).Returns(new List<IZipArchiveEntry> { _mockEntry1.Object, _mockEntry2.Object, mockEntry3.Object });
            _zipFile.Setup(z => z.OpenRead(_zipPath)).Returns(_mockArchive.Object);

            // Act
            var updates = _sut.EnumerateContents(_packageDir, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, updates.Count);
            Assert.Contains("PharosUpdate.msi", updates);
            Assert.Contains("WindowsUpdate.msu", updates);
        }

        [Fact]
        public void ThrowsException_WhenErrorOccursDuringFileEnumeration()
        {
            // Arrange
            _fileSystem.Setup(fs => fs.Directory.EnumerateFiles(_packageDir, "*.zip", SearchOption.AllDirectories))
                      .Throws(new InvalidOperationException("File enumeration error"));

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _sut.EnumerateContents(_packageDir, TestContext.Current.CancellationToken));
        }

        [Fact]
        public void ThrowsException_WhenNoPackagesFound()
        {
            // Arrange
            _fileSystem.Setup(fs => fs.Directory.EnumerateFiles(_packageDir, "*.zip", SearchOption.AllDirectories))
                      .Returns(new List<string>());

            // Act & Assert
            Assert.Throws<InvalidDataException>(() => _sut.EnumerateContents(_packageDir, TestContext.Current.CancellationToken));
        }

        [Fact]
        public void ThrowsException_WhenMultiplePackagesFound()
        {
            // Arrange
            _fileSystem.Setup(fs => fs.Directory.EnumerateFiles(_packageDir, "*.zip", SearchOption.AllDirectories))
                      .Returns(new List<string>
                      {
                          Path.Combine(_packageDir, "PharosUpdatePackage1.zip"),
                          Path.Combine(_packageDir, "PharosUpdatePackage2.zip")
                      });

            // Act & Assert
            Assert.Throws<InvalidDataException>(() => _sut.EnumerateContents(_packageDir, TestContext.Current.CancellationToken));
        }
    }

    public class TheStageToDirectoryMethod : UpdatePackageAccessorTests
    {
        private readonly UpdatePackageAccessor _sut;
        private readonly string _tempExtractRoot;

        public TheStageToDirectoryMethod()
        {
            _tempExtractRoot = Path.Combine(_tempFolder, "Pharos", "SystemUpdate");

            _fileSystem.Setup(fs => fs.Path.GetTempPath()).Returns(_tempFolder);

            _sut = new UpdatePackageAccessor(_zipFile.Object, _fileSystem.Object, Mock.Of<ILogger<UpdatePackageAccessor>>());
        }

        [Fact]
        public void ReturnsStagingDirectory_WhenPackageDirectoryHasValidPackage()
        {
            // Act
            var stagedPath = _sut.StageToDirectory(_packageDir, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(_tempExtractRoot, stagedPath);
        }

        [Fact]
        public void ThrowsException_WhenNoValidPackageFound()
        {
            // Arrange
            _fileSystem.Setup(fs => fs.Directory.EnumerateFiles(_packageDir, "*.zip", SearchOption.AllDirectories))
                      .Returns(new List<string>());

            // Act & Assert
            Assert.Throws<InvalidDataException>(() => _sut.StageToDirectory(_packageDir, TestContext.Current.CancellationToken));
        }

        [Fact]
        public void ClearsStagingFolder_WhenStagingDirectoryExists()
        {
            // Arrange
            _fileSystem.Setup(fs => fs.Directory.Exists(_tempExtractRoot)).Returns(true);

            _fileSystem.Setup(fs => fs.Directory.EnumerateFiles(_tempExtractRoot, "*", SearchOption.AllDirectories)).Returns(new List<string>
            {
                "PharosUpdate.msi",
                "WindowsUpdate.msu"
            });

            _fileSystem.Setup(fs => fs.File.Delete(It.IsAny<string>()));

            // Act
            _ = _sut.StageToDirectory(_packageDir, TestContext.Current.CancellationToken);

            // Assert
            _fileSystem.Verify(fs => fs.File.Delete("PharosUpdate.msi"), Times.Once);
            _fileSystem.Verify(fs => fs.File.Delete("WindowsUpdate.msu"), Times.Once);
        }

        [Fact]
        public void CreatesDirectory_WhenStagingDirectoryDoesNotExist()
        {
            // Arrange
            _fileSystem.Setup(fs => fs.Directory.Exists(_tempExtractRoot)).Returns(false);

            // Act
            _ = _sut.StageToDirectory(_packageDir, TestContext.Current.CancellationToken);

            // Assert
            _fileSystem.Verify(fs => fs.Directory.CreateDirectory(_tempExtractRoot), Times.Once);
        }

        [Fact]
        public void CopiesEntriesToStagingDirectory()
        {
            // Arrange
            _fileSystem.Setup(fs => fs.Directory.Exists(_tempExtractRoot)).Returns(false);

            // Act
            _ = _sut.StageToDirectory(_packageDir, TestContext.Current.CancellationToken);

            // Assert
            _mockEntry1.Verify(e => e.ExtractToFile(Path.Combine(_tempExtractRoot, "PharosUpdate.msi")), Times.Once);
            _mockEntry2.Verify(e => e.ExtractToFile(Path.Combine(_tempExtractRoot, "WindowsUpdate.msu")), Times.Once);
        }
    }

    public class TheFindUpdateCandidatesMethod : UpdatePackageAccessorTests
    {
        private readonly UpdatePackageAccessor _sut;
        public TheFindUpdateCandidatesMethod()
        {
            _fileSystem.Setup(fs => fs.Path.GetTempPath()).Returns(_tempFolder);

            _sut = new UpdatePackageAccessor(_zipFile.Object, _fileSystem.Object, Mock.Of<ILogger<UpdatePackageAccessor>>());
        }
        [Fact]
        public void ReturnsListOfUpdateCandidates_InGivenDirectory()
        {
            // Arrange
            var stagingDirectory = Path.Combine(_tempFolder, "Pharos", "SystemUpdate");
            _fileSystem.Setup(fs => fs.Directory.EnumerateFiles(stagingDirectory, "*.*", SearchOption.TopDirectoryOnly))
                      .Returns(new List<string>
                      {
                          Path.Combine(stagingDirectory, "PharosUpdate.msi"),
                          Path.Combine(stagingDirectory, "WindowsUpdate.msu"),
                          Path.Combine(stagingDirectory, "randomFile.pdf")
                      });

            // Act
            var candidates = _sut.FindUpdateCandidates(stagingDirectory, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, candidates.Count);
            Assert.Contains(Path.Combine(stagingDirectory, "PharosUpdate.msi"), candidates);
            Assert.Contains(Path.Combine(stagingDirectory, "WindowsUpdate.msu"), candidates);
        }
    }
}
