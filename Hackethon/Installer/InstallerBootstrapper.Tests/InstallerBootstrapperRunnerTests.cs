using System;
using Moq;
using Xunit;

namespace InstallerBootstrapper.Tests
{
    public class InstallerBootstrapperRunnerTests
    {
        private const string TestBaseDir = @"C:\Test";

        private readonly Mock<ISystemEnvironment> _env = new();
        private readonly Mock<IFileSystem> _fs = new();
        private readonly Mock<IProcessRunner> _pr = new();
        private readonly Mock<IUserNotifier> _ui = new();
        private readonly InstallerBootstrapperRunner _sut;
        private readonly Mock<IProcessHandle> _process = new();

        public InstallerBootstrapperRunnerTests()
        {
            _env.Setup(e => e.IsAdministrator()).Returns(true);
            _env.Setup(e => e.BaseDirectory).Returns(TestBaseDir);
            _env.Setup(e => e.NowLocal).Returns(new DateTime(2025, 1, 2, 3, 4, 5));
            _env.Setup(e => e.UtcNow).Returns(new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc));

            _fs.Setup(f => f.GetFiles(TestBaseDir, "*.msi")).Returns(Array.Empty<string>());

            _sut = new InstallerBootstrapperRunner(_env.Object, _fs.Object, _pr.Object, _ui.Object);
        }

        [Fact]
        public void Fails_WhenNotAdmin()
        {
            _env.Setup(e => e.IsAdministrator()).Returns(false);

            var rc = _sut.Execute(Array.Empty<string>());

            Assert.NotEqual(0, rc);
            _ui.Verify(u => u.ShowMessage(It.Is<string>(s => s.Contains("Administrator")), "Pharos Installer Bootstrapper"), Times.Once);
            _pr.Verify(p => p.Start(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Fails_OnParseError()
        {
            var rc = _sut.Execute(new[] { "badtoken" });

            Assert.NotEqual(0, rc);
            _ui.Verify(u => u.WriteLine(It.Is<string>(s => s.Contains("Usage:"))), Times.AtLeastOnce);
        }

        [Fact]
        public void Succeeds_OnHelp()
        {
            var rc = _sut.Execute(new[] { "help" });

            Assert.Equal(0, rc);
            _ui.Verify(u => u.WriteLine(It.Is<string>(s => s.Contains("Usage:"))), Times.AtLeastOnce);
        }

        [Fact]
        public void Fails_WhenNoMsiFound()
        {
            var rc = _sut.Execute(Array.Empty<string>());

            Assert.NotEqual(0, rc);
            _ui.Verify(u => u.ShowMessage(It.Is<string>(s => s.Contains("No .msi")), "Pharos Installer Bootstrapper"), Times.Once);
        }

        [Fact]
        public void Succeeds_WithDefaults_AndBuildsExpectedArguments()
        {
            // Arrange
            var msiPath = $"{TestBaseDir}\\PharosInstaller.msi";
            _fs.Setup(f => f.GetFiles(TestBaseDir, "*.msi")).Returns(new[] { msiPath });
            _process.SetupGet(p => p.ExitCode).Returns(0);
            _process.Setup(p => p.WaitForExit());
            _pr.Setup(p => p.Start("msiexec.exe", It.IsAny<string>())).Returns(_process.Object);

            // Act
            var rc = _sut.Execute(Array.Empty<string>());

            // Assert
            Assert.Equal(0, rc);
            _pr.Verify(p => p.Start("msiexec.exe",
                It.Is<string>(s =>
                    s.Contains($"/i \"{msiPath}\"") &&
                    s.Contains("/passive") &&
                    s.Contains("/lewimo!") &&
                    s.Contains("secureenclavesenabled=true") &&
                    !s.Contains("testrollback=true")
                )), Times.Once);
            _fs.Verify(f => f.CreateDirectory(InstallerBootstrapperRunner.LogFolder), Times.Once);
            _ui.Verify(u => u.WriteLine("Installation succeeded."), Times.Once);
        }

        [Fact]
        public void Fails_WhenTestRollbackIsSet()
        {
            // Arrange
            var msiPath = $"{TestBaseDir}\\PharosInstaller.msi";
            _fs.Setup(f => f.GetFiles(TestBaseDir, "*.msi")).Returns(new[] { msiPath });
            _process.SetupGet(p => p.ExitCode).Returns(123); // any non-zero
            _pr.Setup(p => p.Start("msiexec.exe", It.IsAny<string>())).Returns(_process.Object);

            // Act
            var rc = _sut.Execute(new[] { "testrollback=true", "secureenclavesenabled=false" });

            // Assert
            Assert.NotEqual(0, rc);
            _ui.Verify(u => u.WriteLine(It.Is<string>(m => m.Contains("failed as expected"))), Times.Once);
            _ui.Verify(u => u.ShowMessage(It.IsAny<string>(), "Pharos Installation Failed"), Times.Never);
        }

        [Fact]
        public void FailsAndShowsDetailedFailureInfo_WhenNonZeroExitAndNotTestingRollback()
        {
            var msiPath = $"{TestBaseDir}\\PharosInstaller.msi";
            _fs.Setup(f => f.GetFiles(TestBaseDir, "*.msi")).Returns(new[] { msiPath });
            _process.SetupGet(p => p.ExitCode).Returns(5);
            _pr.Setup(p => p.Start("msiexec.exe", It.IsAny<string>())).Returns(_process.Object);

            var rc = _sut.Execute(Array.Empty<string>());

            Assert.NotEqual(0, rc);
            _ui.Verify(u => u.ShowMessage(It.Is<string>(s => s.Contains("Pharos installation failed")),
                "Pharos Installation Failed"), Times.Once);
        }
    }
}