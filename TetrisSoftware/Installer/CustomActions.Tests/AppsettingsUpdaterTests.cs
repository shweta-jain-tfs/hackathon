using System.IO;
using CustomAction.CustomActionLogic.Appsettings;
using CustomAction.CustomActionLogic.Interfaces.AppSettings;
using CustomAction.Wrappers.Interfaces;
using Moq;
using Xunit;

namespace CustomActions.Tests
{
    public class AppSettingsUpdaterTests
    {
        const string _appsettingsFileName = "TestAppsettings.json";
        private readonly AppSettingsUpdater _sut;
        private readonly Mock<IAppSettingsLocator> _mockAppSettingsLocator;
        private readonly Mock<IAppSettingsOverrideOptions> _mockOptions;
        private readonly Mock<IFileSystem> _mockFileSystem;

        protected AppSettingsUpdaterTests()
        {
            var testAppsettings = File.ReadAllText(_appsettingsFileName);
            var mockSession = new Mock<ISessionWrapper>();

            _mockFileSystem = new Mock<IFileSystem>();
            _mockFileSystem.Setup(f => f.ReadAllText(_appsettingsFileName)).Returns(testAppsettings);

            _mockAppSettingsLocator = new Mock<IAppSettingsLocator>();
            _mockAppSettingsLocator.Setup(a => a.AppSettingsPath).Returns(_appsettingsFileName);

            _mockOptions = new Mock<IAppSettingsOverrideOptions>();

            _sut = new AppSettingsUpdater(mockSession.Object, _mockFileSystem.Object, _mockAppSettingsLocator.Object, _mockOptions.Object);
        }
        public class TheUpdateDataProtectionSettingMethod : AppSettingsUpdaterTests
        {
            [Theory]
            [InlineData("true")]
            [InlineData("false")]
            public void UpdatesDataProtectionSetting(string enableDataProtectionValue)
            {
                // Arrange
                _mockOptions.Setup(o => o.EnableDataProtection).Returns(enableDataProtectionValue);

                var expectedAppsettings = "";

                _mockFileSystem.Setup(m => m.WriteAllText(_appsettingsFileName, It.IsAny<string>())).Callback<string, string>((path, content) =>
                {
                    expectedAppsettings = content;
                });

                // Act
                _sut.UpdateDataProtectionSetting();

                // Assert
                Assert.Contains($"\"EnableDataProtection\": {enableDataProtectionValue.ToLower()}", expectedAppsettings);
            }
        }
    }
}