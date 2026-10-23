using System.IO;
using CustomAction.CustomActionLogic;
using CustomAction.CustomActionLogic.Interfaces.AppSettings;
using CustomAction.Wrappers.Interfaces;
using Moq;
using WixToolset.Dtf.WindowsInstaller;
using Xunit;

namespace CustomActions.Tests
{
    public class AppSettingsActionsTests
    {
        public class TheUpdateAppSettingsMethod
        {
            private readonly AppSettingsActions _sut;
            private readonly Mock<IAppSettingsUpdater> _appSettingsUpdaterMock;

            public TheUpdateAppSettingsMethod()
            {
                var mockSession = new Mock<ISessionWrapper>();

                _appSettingsUpdaterMock = new Mock<IAppSettingsUpdater>();

                _sut = new AppSettingsActions(mockSession.Object, _appSettingsUpdaterMock.Object);
            }

            [Fact]
            public void UpdatesDataProtectionSetting()
            {
                // Act
                _ = _sut.UpdateAppSettings();

                // Assert
                _appSettingsUpdaterMock.Verify(a => a.UpdateDataProtectionSetting(), Times.Once);
            }

            [Fact]
            public void ReturnsSuccessWhenNoExceptionIsThrown()
            {
                // Act
                var result = _sut.UpdateAppSettings();
                // Assert
                Assert.Equal(ActionResult.Success, result);
            }

            [Fact]
            public void ReturnsFailureWhenExceptionIsThrown_FromDataProtectionUpdate()
            {
                // Arrange
                _appSettingsUpdaterMock.Setup(a => a.UpdateDataProtectionSetting()).Throws(new IOException("Test exception"));
                // Act
                var result = _sut.UpdateAppSettings();
                // Assert
                Assert.Equal(ActionResult.Failure, result);
            }
        }
    }
}