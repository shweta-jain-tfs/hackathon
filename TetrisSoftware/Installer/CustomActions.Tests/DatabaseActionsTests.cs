using System;
using System.IO;
using CustomAction.CustomActionLogic;
using CustomAction.CustomActionLogic.Interfaces.Database;
using CustomAction.Wrappers.Interfaces;
using Moq;
using WixToolset.Dtf.WindowsInstaller;
using Xunit;

namespace CustomActions.Tests
{
    public class DatabaseActionsTests
    {
        private readonly DatabaseActions _sut;
        private readonly Mock<IFileSystem> _fileSystem;
        private readonly Mock<IDatabaseInstanceFactory> _databaseInstanceFactory;
        private readonly Mock<IDate> _date;
        private readonly Mock<IEnvironment> _environment;
        private readonly Mock<IDatabaseInstance> _databaseInstance;
        const string _serviceAccount = "NT AUTHORITY\\SYSTEM";

        protected DatabaseActionsTests()
        {
            var session = new Mock<ISessionWrapper>();
            _fileSystem = new Mock<IFileSystem>();
            _databaseInstanceFactory = new Mock<IDatabaseInstanceFactory>();
            _databaseInstance = new Mock<IDatabaseInstance>();
            _databaseInstanceFactory.Setup(m => m.CreateInstance()).Returns(_databaseInstance.Object);

            _date = new Mock<IDate>();
            _environment = new Mock<IEnvironment>();

            _environment.Setup(e => e.GetCommonAppDataPath()).Returns("C:\\ProgramData");

            var options = new Mock<IServiceAccountOptions>();
            options.SetupGet(o => o.ServiceAccount).Returns(_serviceAccount);

            _sut = new DatabaseActions(
                session.Object,
                _fileSystem.Object,
                _environment.Object,
                _databaseInstanceFactory.Object,
                _date.Object,
                options.Object);
        }

        public class TheBackupDatabaseMethod : DatabaseActionsTests
        {
            const string _expectedBackupFolder = @"C:\ProgramData\Thermo Fisher Scientific\Pharos\DatabaseBackups";

            public TheBackupDatabaseMethod()
            {
                _environment.Setup(m => m.GetCommonAppDataPath()).Returns(@"C:\ProgramData");
            }

            [Fact]
            public void ReturnsSuccessWhenBackupSucceeds()
            {
                // Arrange
                _databaseInstance.Setup(m => m.BackupIfExists(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
                // Act
                var result = _sut.BackupDatabase();
                // Assert
                Assert.Equal(ActionResult.Success, result);
            }

            [Fact]
            public void ReturnsFailureWhenBackupThrows()
            {
                // Arrange
                _databaseInstance.Setup(m => m.BackupIfExists(It.IsAny<string>(), It.IsAny<string>())).Throws<IOException>();
                // Act
                var result = _sut.BackupDatabase();
                // Assert
                Assert.Equal(ActionResult.Failure, result);
            }

            [Fact]
            public void CreatesBackupFolderToExpectedLocation()
            {
                // Act
                _sut.BackupDatabase();

                // Assert
                _fileSystem.Verify(m => m.CreateDirectory(_expectedBackupFolder), Times.Once);
            }

            [Fact]
            public void DeletesExistingBackupFolder()
            {
                // Arrange
                _fileSystem.Setup(m => m.DirectoryExists(_expectedBackupFolder)).Returns(true);

                // Act
                _sut.BackupDatabase();

                // Assert
                _fileSystem.Verify(m => m.DeleteDirectory(_expectedBackupFolder), Times.Once);
            }

            [Fact]
            public void CreatesNewBackupFolderAfterDeletingExistingOne()
            {
                // Arrange
                _fileSystem.Setup(m => m.DirectoryExists(_expectedBackupFolder)).Returns(true);

                // Act
                _sut.BackupDatabase();

                // Assert
                _fileSystem.Verify(m => m.DeleteDirectory(_expectedBackupFolder), Times.Once);
                _fileSystem.Verify(m => m.CreateDirectory(_expectedBackupFolder), Times.Once);
            }

            [Fact]
            public void NamesBackupFileInCorrectFormat()
            {
                // Arrange
                const string expectedFileName = "PharosDBBackup-2021-01-01-0101.bak";
                var expectedFullName = _expectedBackupFolder + "\\" + expectedFileName;

                var actualFullName = "";

                _date.Setup(d => d.Now).Returns(new DateTime(2021, 1, 1, 1, 1, 0));

                _databaseInstance.Setup(m => m.BackupIfExists(It.IsAny<string>(), It.IsAny<string>())).Callback((string dbName, string backupFullPath) =>
                {
                    actualFullName = backupFullPath;
                }).Returns(true);

                // Act
                _sut.BackupDatabase();

                // Assert
                Assert.Equal(expectedFullName, actualFullName);
            }

            [Fact]
            public void BackupsCorrectDatabase()
            {
                // Arrange
                const string expectedDbName = "Pharos";
                var actualDbName = "";

                _databaseInstance.Setup(m => m.BackupIfExists(It.IsAny<string>(), It.IsAny<string>())).Callback((string dbName, string backupFullPath) =>
                {
                    actualDbName = dbName;
                }).Returns(true);

                // Act
                _sut.BackupDatabase();

                // Assert
                Assert.Equal(expectedDbName, actualDbName);
            }
        }

        public class TheAddWindowsServiceAccountAsSqlLoginMethod : DatabaseActionsTests
        {
            [Fact]
            public void AddsWindowsServiceAccountAsLogin()
            {
                // Arrange
                var expectedFullName = _serviceAccount;

                // Act
                _sut.AddWindowsServiceAccountAsSqlLogin();

                // Assert
                _databaseInstance.Verify(m => m.AddWindowsUserAsLogin(expectedFullName), Times.Once);
            }

            [Fact]
            public void ReturnsFailureWhenAddingWindowsServiceAccountAsLoginFails()
            {
                // Arrange
                _databaseInstance.Setup(m => m.AddWindowsUserAsLogin(It.IsAny<string>())).Throws<Exception>();

                // Act
                var result = _sut.AddWindowsServiceAccountAsSqlLogin();

                // Assert
                Assert.Equal(ActionResult.Failure, result);
            }

            [Fact]
            public void AddsWindowsServiceAccountToDbCreatorRole()
            {
                // Arrange
                var expectedFullName = _serviceAccount;

                // Act
                _sut.AddWindowsServiceAccountAsSqlLogin();

                // Assert
                _databaseInstance.Verify(m => m.AddUserToRole(expectedFullName, "dbcreator"), Times.Once);
            }
        }
        public class TheRestoreDatabaseBackupMethod : DatabaseActionsTests
        {
            const string _expectedBackupFolder = @"C:\ProgramData\Thermo Fisher Scientific\Pharos\DatabaseBackups";

            public TheRestoreDatabaseBackupMethod()
            {
                _environment.Setup(m => m.GetCommonAppDataPath()).Returns(@"C:\ProgramData");
            }

            [Fact]
            public void ReturnsFailure_WhenBackupFolderDoesNotExist()
            {
                // Arrange
                _fileSystem.Setup(m => m.DirectoryExists(_expectedBackupFolder)).Returns(false);

                // Act
                var result = _sut.RestoreDatabaseBackup();

                // Assert
                Assert.Equal(ActionResult.Failure, result);
            }

            [Fact]
            public void ReturnsSuccess_WhenBackupFileExistsAndRestoreSucceeds()
            {
                // Arrange
                const string fileName = "PharosDBBackup-2021-01-01-0101.bak";

                _fileSystem.Setup(fs => fs.DirectoryExists(_expectedBackupFolder)).Returns(true);
                var backupFile = _expectedBackupFolder + "\\" + fileName;
                _fileSystem.Setup(fs => fs.GetFiles(_expectedBackupFolder, "*.bak")).Returns(new[] { backupFile });

                _databaseInstance.Setup(db => db.RestoreDatabaseBackup(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

                // Act
                var result = _sut.RestoreDatabaseBackup();

                // Assert
                _databaseInstance.Verify(m => m.RestoreDatabaseBackup("Pharos", backupFile), Times.Once);
                Assert.Equal(ActionResult.Success, result);
            }

            [Fact]
            public void DoesNotRestoreBackup_WhenNoBackupsExist()
            {
                // Arrange
                _fileSystem.Setup(fs => fs.DirectoryExists(_expectedBackupFolder)).Returns(true);
                _fileSystem.Setup(fs => fs.GetFiles(_expectedBackupFolder, "*.bak")).Returns(Array.Empty<string>());

                // Act
                var result = _sut.RestoreDatabaseBackup();

                // Assert
                _databaseInstance.Verify(m => m.RestoreDatabaseBackup(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
                Assert.Equal(ActionResult.Success, result);
            }


            [Fact]
            public void ReturnsFailure_WhenExceptionIsThrown()
            {
                // Arrange
                const string fileName = "PharosDBBackup-2021-01-01-0101.bak";
                _fileSystem.Setup(fs => fs.DirectoryExists(_expectedBackupFolder)).Returns(true);
                var backupFile = _expectedBackupFolder + "\\" + fileName;
                _fileSystem.Setup(fs => fs.GetFiles(_expectedBackupFolder, "*.bak")).Returns(new[] { backupFile });

                _databaseInstance.Setup(db => db.RestoreDatabaseBackup(It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("restore error"));

                // Act
                var result = _sut.RestoreDatabaseBackup();

                // Assert
                Assert.Equal(ActionResult.Failure, result);
            }
        }

        public class TheEnableSecureEnclaveIfDisabledMethod : DatabaseActionsTests
        {
            [Fact]
            public void EnablesSecureEnclave_AndRestartsSqlServer_WhenItIsDisabled()
            {
                // Arrange
                _databaseInstance.Setup(m => m.IsSecureEnclaveEnabled()).Returns(false);

                // Act
                _sut.EnableSecureEnclaveIfDisabled();

                // Assert
                _databaseInstance.Verify(m => m.EnableSecureEnclave(), Times.Once);
                _databaseInstance.Verify(m => m.RestartSqlServerService(), Times.Once);
            }

            [Fact]
            public void DoesNotEnableSecureEnclave_WhenItIsAlreadyEnabled()
            {
                // Arrange
                _databaseInstance.Setup(m => m.IsSecureEnclaveEnabled()).Returns(true);

                // Act
                _sut.EnableSecureEnclaveIfDisabled();

                // Assert
                _databaseInstance.Verify(m => m.EnableSecureEnclave(), Times.Never);
            }

            [Fact]
            public void ReturnsFailure_WhenExceptionIsThrown()
            {
                // Arrange
                _databaseInstance.Setup(m => m.IsSecureEnclaveEnabled()).Throws<Exception>();

                // Act
                var result = _sut.EnableSecureEnclaveIfDisabled();

                // Assert
                Assert.Equal(ActionResult.Failure, result);
            }
        }
    }
}