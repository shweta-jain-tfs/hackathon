using System;
using System.Data;
using CustomAction.CustomActionLogic.Database;
using CustomAction.CustomActionLogic.Interfaces.Database;
using CustomAction.Wrappers.Interfaces;
using Moq;
using Xunit;

namespace CustomActions.Tests
{
    public class DatabaseInstanceTests
    {
        private readonly DatabaseInstance _sut;
        private readonly Mock<IDbCommandFactory> _dbCommandFactoryMock;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommandWrapper> _dbCommandMock;
        private readonly Mock<IDbConnectionFactory> _dbConnectionFactoryMock;
        private readonly Mock<ISessionWrapper> _sessionWrapperMock;
        private const string _connectionString = "ConnectionStringToDatabase";

        protected DatabaseInstanceTests()
        {
            _dbConnectionFactoryMock = new Mock<IDbConnectionFactory>();
            _dbCommandFactoryMock = new Mock<IDbCommandFactory>();
            _sessionWrapperMock = new Mock<ISessionWrapper>();

            _dbCommandMock = new Mock<IDbCommandWrapper>();
            _dbCommandFactoryMock.Setup(m => m.CreateCommand(It.IsAny<string>(), It.IsAny<IDbConnection>())).Returns(_dbCommandMock.Object);

            _mockConnection = new Mock<IDbConnection>();
            _dbConnectionFactoryMock.Setup(m => m.CreateConnection(It.IsAny<string>())).Returns(_mockConnection.Object);

            _sut = new DatabaseInstance(_dbConnectionFactoryMock.Object, _dbCommandFactoryMock.Object, _sessionWrapperMock.Object, _connectionString);
        }

        // Number of spaces and new lines do not affect the behavior of the query
        private string NormalizeQuery(string query)
        {
            return query.Replace(" ", "").Replace("\r", "").Replace("\n", "");
        }

        public class TheAddWindowsUserAsLoginMethod : DatabaseInstanceTests
        {
            [Fact]
            public void OpensConnection()
            {
                // Act
                _sut.AddWindowsUserAsLogin(@"DESKTOP-1234\TestUser");

                // Assert
                _mockConnection.Verify(m => m.Open(), Times.Once);
            }

            [Fact]
            public void ClosesConnection()
            {
                // Act
                _sut.AddWindowsUserAsLogin(@"DESKTOP-1234\TestUser");

                // Assert
                _mockConnection.Verify(m => m.Close(), Times.Once);
            }

            [Fact]
            public void CreatesCorrectConnection() {

                // Act
                _sut.AddWindowsUserAsLogin(@"DESKTOP-1234\TestUser");
                // Assert
                _dbConnectionFactoryMock.Verify(m => m.CreateConnection(_connectionString), Times.Once);
            }

            [Fact]
            public void ExecutesAddUserAsWindowsLoginQuery()
            {
                // Arrange
                var userName = @"DESKTOP-1234\TestUser";

                var expectedQuery = $"IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @userName) BEGIN EXEC sp_executesql N'CREATE LOGIN [{userName}] FROM WINDOWS'; END;";

                var actualQuery = "";

                _dbCommandFactoryMock.Setup(m => m.CreateCommand(It.IsAny<string>(), It.IsAny<IDbConnection>())).Returns(_dbCommandMock.Object).Callback((string query, IDbConnection connection) =>
                {
                    actualQuery = query;
                });

                // Act
                _sut.AddWindowsUserAsLogin(userName);

                // Assert without tabs
                Assert.Equal(NormalizeQuery(expectedQuery), NormalizeQuery(actualQuery));
                _dbCommandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
            }

            [Theory]
            [InlineData("")]
            [InlineData(" ")]
            [InlineData(null)]
            [InlineData("'; DROP TABLE users; --")]
            [InlineData("admin'--")]
            [InlineData("' OR '1'='1")]
            public void ThrowsExceptionWhenInvalidUserNameProvided(string userName)
            {
                // Act
                void Act() => _sut.AddWindowsUserAsLogin(userName);

                // Assert
                Assert.Throws<ArgumentException>(Act);
            }

            [Fact]
            public void AddsUserNameParameterToSqlCommand()
            {
                // Arrange
                var userName = @"DESKTOP-1234\TestUser";

                // Act
                _sut.AddWindowsUserAsLogin(userName);

                // Assert
                _dbCommandMock.Verify(m => m.AddParameter("userName", userName), Times.Once);
            }
        }

        public class TheAddUserToRoleMethod : DatabaseInstanceTests
        {
            [Fact]
            public void OpensConnection()
            {
                // Act
                _sut.AddUserToRole(@"DESKTOP-1234\TestUser", "db_owner");

                // Assert
                _mockConnection.Verify(m => m.Open(), Times.Once);
            }

            [Fact]
            public void ClosesConnection()
            {
                // Act
                _sut.AddUserToRole(@"DESKTOP-1234\TestUser", "db_owner");

                // Assert
                _mockConnection.Verify(m => m.Close(), Times.Once);
            }

            [Fact]
            public void CreatesCorrectConnection()
            {
                // Act
                _sut.AddUserToRole(@"DESKTOP-1234\TestUser", "db_owner");
                // Assert
                _dbConnectionFactoryMock.Verify(m => m.CreateConnection(_connectionString), Times.Once);
            }

            [Fact]
            public void ExecutesAddUserToRoleQuery()
            {
                // Arrange
                var userName = @"DESKTOP-1234\TestUser";
                var role = "db_owner";

                var expectedQuery = $@"
        IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @userName)
           AND NOT EXISTS (
                SELECT 1
                FROM sys.server_role_members rm
                JOIN sys.server_principals r ON rm.role_principal_id = r.principal_id
                JOIN sys.server_principals m ON rm.member_principal_id = m.principal_id
                WHERE r.name = @role AND m.name = @userName
           )
        BEGIN
            EXEC sp_executesql N'ALTER SERVER ROLE {role} ADD MEMBER [{userName}]';
        END;";

                var actualQuery = "";

                _dbCommandFactoryMock.Setup(m => m.CreateCommand(It.IsAny<string>(), It.IsAny<IDbConnection>())).Returns(_dbCommandMock.Object).Callback((string query, IDbConnection connection) =>
                {
                    actualQuery = query;
                });

                // Act
                _sut.AddUserToRole(userName, role);

                // Assert
                Assert.Equal(NormalizeQuery(expectedQuery), NormalizeQuery(actualQuery));
                _dbCommandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
            }

            [Theory]
            [InlineData("")]
            [InlineData(" ")]
            [InlineData(null)]
            [InlineData("'; DROP TABLE users; --")]
            [InlineData("admin'--")]
            [InlineData("' OR '1'='1")]
            public void ThrowsExceptionWhenInvalidUserNameProvided(string userName)
            {
                // Act
                void Act() => _sut.AddUserToRole(userName, "db_owner");

                // Assert
                Assert.Throws<ArgumentException>(Act);
            }

            [Theory]
            [InlineData("")]
            [InlineData(" ")]
            [InlineData(null)]
            [InlineData("'; DROP TABLE users; --")]
            [InlineData("admin'--")]
            [InlineData("' OR '1'='1")]
            public void ThrowsExceptionWhenInvalidRoleProvided(string role)
            {
                // Act
                void Act() => _sut.AddUserToRole(@"DESKTOP-1234\TestUser", role);

                // Assert
                Assert.Throws<ArgumentException>(Act);
            }

            [Fact]
            public void AddsUserNameParameterToSqlCommand()
            {
                // Arrange
                var userName = @"DESKTOP-1234\TestUser";

                // Act
                _sut.AddUserToRole(userName, "db_owner");

                // Assert
                _dbCommandMock.Verify(m => m.AddParameter("userName", userName), Times.Once);
            }
        }

        public class TheBackupIfExistsMethod : DatabaseInstanceTests
        {
            [Fact]
            public void OpensConnection()
            {
                // Act
                _sut.BackupIfExists("Pharos", "C:\\backup.bak");

                // Assert
                _mockConnection.Verify(m => m.Open(), Times.Once);
            }

            [Fact]
            public void ClosesConnection()
            {
                // Act
                _sut.BackupIfExists("Pharos", "C:\\backup.bak");

                // Assert
                _mockConnection.Verify(m => m.Close(), Times.Once);
            }

            [Fact]
            public void CreatesCorrectConnection()
            {
                // Act
                _sut.BackupIfExists("Pharos", "C:\\backup.bak");
                // Assert
                _dbConnectionFactoryMock.Verify(m => m.CreateConnection(_connectionString), Times.Once);
            }

            [Fact]
            public void ExecutesBackupQuery()
            {
                // Arrange
                var expectedQuery =
                        @"
                        IF EXISTS(SELECT 1 FROM master.dbo.sysdatabases where name=@dbName)
                        BEGIN
                           SELECT 1
                        BACKUP DATABASE @dbName
                        TO DISK = @path
                        WITH
                            INIT,
                            NAME = @dbName
                        END
                        ELSE
                        BEGIN
                            SELECT 0
                        END;
                    ";

                var actualQuery = "";

                _dbCommandFactoryMock.Setup(m => m.CreateCommand(It.IsAny<string>(), It.IsAny<IDbConnection>())).Returns(_dbCommandMock.Object).Callback((string query, IDbConnection connection) =>
                {
                    actualQuery = query;
                });

                // Act
                _sut.BackupIfExists("Pharos", "C:\\backup.bak");

                // Assert
                Assert.Equal(NormalizeQuery(expectedQuery), NormalizeQuery(actualQuery));
                _dbCommandMock.Verify(m => m.ExecuteScalar(), Times.Once);
            }

            [Fact]
            public void AddsDbNameParameterToSqlCommand()
            {
                // Arrange
                var dbName = "Pharos";

                // Act
                _sut.BackupIfExists(dbName, "C:\\backup.bak");

                // Assert
                _dbCommandMock.Verify(m => m.AddParameter("dbName", dbName), Times.Once);
            }

            [Fact]
            public void AddsPathParameterToSqlCommand()
            {
                // Arrange
                var path = "C:\\backup.bak";

                // Act
                _sut.BackupIfExists("Pharos", path);

                // Assert
                _dbCommandMock.Verify(m => m.AddParameter("path", path), Times.Once);
            }
        }

        public class TheIsSecureEnclaveEnabledMethod : DatabaseInstanceTests
        {
            [Theory]
            [InlineData(1, true)]
            [InlineData(0, false)]
            public void ReturnsExpectedResultBasedOnConfigurationValue(object dbValue, bool expected)
            {
                // Arrange  
                _dbCommandMock.Setup(m => m.ExecuteScalar()).Returns(Convert.ToBoolean(dbValue));

                // Act  
                var result = _sut.IsSecureEnclaveEnabled();

                // Assert  
                Assert.Equal(expected, result);
                _mockConnection.Verify(m => m.Open(), Times.Once);
                _mockConnection.Verify(m => m.Close(), Times.Once);
                _dbCommandMock.Verify(m => m.ExecuteScalar(), Times.Once);
            }
        }

        public class TheEnableSecureEnclaveMethod : DatabaseInstanceTests
        {
            [Fact]
            public void OpensConnection()
            {
                // Act
                _sut.EnableSecureEnclave();
                // Assert
                _mockConnection.Verify(m => m.Open(), Times.Once);
            }

            [Fact]
            public void ClosesConnection()
            {
                // Act
                _sut.EnableSecureEnclave();
                // Assert
                _mockConnection.Verify(m => m.Close(), Times.Once);
            }

            [Fact]
            public void CreatesCorrectConnection()
            {
                // Act
                _sut.EnableSecureEnclave();
                // Assert
                _dbConnectionFactoryMock.Verify(m => m.CreateConnection(_connectionString), Times.Once);
            }

            [Fact]
            public void ExecutesEnableSecureEnclaveCommand()
            {
                // Arrange
                const string expectedQuery = "EXEC sp_configure 'column encryption enclave type', 1; RECONFIGURE;";
                var actualQuery = string.Empty;
                _dbCommandFactoryMock.Setup(m => m.CreateCommand(It.IsAny<string>(), It.IsAny<IDbConnection>()))
                    .Returns(_dbCommandMock.Object)
                    .Callback((string query, IDbConnection connection) =>
                    {
                        actualQuery = query;
                    });

                // Act
                _sut.EnableSecureEnclave();

                // Assert
                Assert.Equal(NormalizeQuery(expectedQuery), NormalizeQuery(actualQuery));
                _dbCommandMock.Verify(m => m.ExecuteNonQuery(), Times.Once);
            }

            [Fact]
            public void ThrowsOnException()
            {
                // Arrange
                _dbCommandMock.Setup(m => m.ExecuteNonQuery()).Throws(new Exception("fail"));

                // Act & Assert
                Assert.Throws<Exception>(() => _sut.EnableSecureEnclave());
            }
        }

        public class TheRestoreDatabaseBackupMethod : DatabaseInstanceTests
        {
            [Fact]
            public void OpensConnection()
            {
                // Act
                _sut.RestoreDatabaseBackup("Pharos", "C:\\backup.bak");
                // Assert
                _mockConnection.Verify(m => m.Open(), Times.Once);
            }

            [Fact]
            public void ClosesConnection() {
                // Act
                _sut.RestoreDatabaseBackup("Pharos", "C:\\backup.bak");
                // Assert
                _mockConnection.Verify(m => m.Close(), Times.Once);
            }

            [Fact]
            public void CreatesCorrectConnection()
            {
                // Act
                _sut.RestoreDatabaseBackup("Pharos", "C:\\backup.bak");
                // Assert
                _dbConnectionFactoryMock.Verify(m => m.CreateConnection(_connectionString), Times.Once);
            }

            [Fact]
            public void ExecutesRestoreQuery()
            {
                // Arrange
                var expectedQuery =
                        @"
                        IF EXISTS (SELECT 1 FROM master.dbo.sysdatabases WHERE name = @dbName)
                        BEGIN
                            RESTORE DATABASE @dbName
                            FROM DISK = @path
                            WITH REPLACE;
                            SELECT 1;
                        END
                        ELSE
                        BEGIN
                            SELECT 0;
                        END";

                var actualQuery = "";

                _dbCommandFactoryMock.Setup(m => m.CreateCommand(It.IsAny<string>(), It.IsAny<IDbConnection>())).Returns(_dbCommandMock.Object).Callback((string query, IDbConnection connection) =>
                {
                    actualQuery = query;
                });

                // Act
                _sut.RestoreDatabaseBackup("Pharos", "C:\\backup.bak");

                // Assert
                Assert.Equal(NormalizeQuery(expectedQuery), NormalizeQuery(actualQuery));
                _dbCommandMock.Verify(m => m.ExecuteScalar(), Times.Once);
            }

            [Fact]
            public void AddsDbNameParameterToSqlCommand()
            {
                // Arrange
                var dbName = "Pharos";

                // Act
                _sut.RestoreDatabaseBackup(dbName, "C:\\backup.bak");

                // Assert
                _dbCommandMock.Verify(m => m.AddParameter("dbName", dbName), Times.Once);
            }

            [Fact]
            public void AddsPathParameterToSqlCommand()
            {
                // Arrange
                var path = "C:\\backup.bak";

                // Act
                _sut.RestoreDatabaseBackup("Pharos", path);

                // Assert
                _dbCommandMock.Verify(m => m.AddParameter("path", path), Times.Once);
            }

            [Fact]
            public void ReturnsTrueWhenRestoreSucceeds()
            {
                _dbCommandMock.Setup(m => m.ExecuteScalar()).Returns(true);
                var result = _sut.RestoreDatabaseBackup("Pharos", "C:\\backup.bak");
                Assert.True(result);
            }

            [Fact]
            public void ThrowsWhenCommandFails()
            {
                _dbCommandMock.Setup(m => m.ExecuteScalar()).Throws(new Exception("fail"));
                Assert.Throws<Exception>(() => _sut.RestoreDatabaseBackup("Pharos", "C:\\backup.bak"));
            }
        }
    }
}