using System;
using System.IO;
using CustomAction.CustomActionLogic;
using CustomAction.Wrappers.Interfaces;
using Moq;
using WixToolset.Dtf.WindowsInstaller;
using Xunit;

namespace CustomActions.Tests
{
    public class ApplicationInitializationActionsTests
    {
        private readonly ApplicationInitializationActions _sut;
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly Mock<IProcessRunner> _processRunner;

        protected ApplicationInitializationActionsTests()
        {
            var session = new Mock<ISessionWrapper>();
            _fileSystem = new Mock<IFileSystem>();
            _processRunner = new Mock<IProcessRunner>();

            _sut = new ApplicationInitializationActions(session.Object, _fileSystem.Object, _processRunner.Object);
        }

        public class TheRunInitDbMethod : ApplicationInitializationActionsTests
        {
            const string _installFolder = @"C:\Program Files\Thermo Fisher Scientific\Pharos";
            readonly string _exePath = Path.Combine(_installFolder, "Pharos.Server.exe");

            public TheRunInitDbMethod()
            {
                _fileSystem.Setup(m => m.Exists(_exePath)).Returns(true);
            }

            [Fact]
            public void ReturnsFailure_WhenInstallFolderIsNullOrWhitespace()
            {
                // Act
                var result = _sut.RunInitDb(" ");

                // Assert
                Assert.Equal(ActionResult.Failure, result);
                _processRunner.Verify(m => m.Run(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }

            [Fact]
            public void ReturnsFailure_WhenExeDoesNotExist()
            {
                // Arrange
                _fileSystem.Setup(m => m.Exists(_exePath)).Returns(false);

                // Act
                var result = _sut.RunInitDb(_installFolder);

                // Assert
                Assert.Equal(ActionResult.Failure, result);
                _processRunner.Verify(m => m.Run(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }

            [Fact]
            public void ReturnsFailure_WhenProcessReturnsNonZeroExitCode()
            {
                // Arrange
                _processRunner.Setup(m => m.Run(_exePath, "--init-db", _installFolder)).Returns(1);

                // Act
                var result = _sut.RunInitDb(_installFolder);

                // Assert
                Assert.Equal(ActionResult.Failure, result);
                _processRunner.Verify(m => m.Run(_exePath, "--init-db", _installFolder), Times.Once);
            }

            [Fact]
            public void ReturnsSuccess_WhenProcessReturnsZeroExitCode()
            {
                // Arrange
                _processRunner.Setup(m => m.Run(_exePath, "--init-db", _installFolder)).Returns(0);

                // Act
                var result = _sut.RunInitDb(_installFolder);

                // Assert
                Assert.Equal(ActionResult.Success, result);
                _processRunner.Verify(m => m.Run(_exePath, "--init-db", _installFolder), Times.Once);
            }

            [Fact]
            public void ReturnsFailure_WhenExceptionIsThrown()
            {
                // Arrange
                _processRunner
                    .Setup(m => m.Run(_exePath, "--init-db", _installFolder))
                    .Throws(new Exception("boom"));

                // Act
                var result = _sut.RunInitDb(_installFolder);

                // Assert
                Assert.Equal(ActionResult.Failure, result);
                _processRunner.Verify(m => m.Run(_exePath, "--init-db", _installFolder), Times.Once);
            }
        }
    }
}
