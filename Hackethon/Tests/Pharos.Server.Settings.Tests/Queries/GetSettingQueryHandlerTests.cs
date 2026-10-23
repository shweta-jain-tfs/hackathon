using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.General;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Queries;

namespace Pharos.Server.Settings.Queries;

public class GetSettingQueryHandlerTests
{
    private readonly Mock<ILogger<GetSettingQueryHandler>> _loggerMock;
    private readonly Mock<ISettingsPermissionHelper> _permissionHelperMock;
    private readonly Mock<ISettingsPagesLoader> _loaderMock;

    public GetSettingQueryHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GetSettingQueryHandler>>();
        _permissionHelperMock = new Mock<ISettingsPermissionHelper>();
        _loaderMock = new Mock<ISettingsPagesLoader>();
    }

    [Fact]
    public async Task Handle_ShouldPopulateDynamicDropdowns_WhenProviderIsRegistered()
    {
        // Arrange
        var timeZoneProvider = new TimeZoneDropdownProvider();
        var providers = new List<IDropdownValueProvider> { timeZoneProvider };

        var option = new Option
        {
            Key = "GeneralSettings:RegionalSettings:TimeZone",
            Type = OptionType.Choice,
            Value = "UTC",
            DynamicDropdownProviderType = typeof(TimeZoneDropdownProvider),
            Choices = []
        };

        var page = new Page
        {
            Key = "GeneralSettings:RegionalSettings",
            Label = "Regional Settings",
            Children = [option]
        };

        var populatedPages = new Dictionary<string, Page>
        {
            [page.Key] = page
        };

        _loaderMock
            .Setup(x => x.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(populatedPages);

        _permissionHelperMock
            .Setup(x => x.RemoveInaccessibleChildrenAsync(It.IsAny<Page>()))
            .Returns(Task.CompletedTask);

        var handler = new GetSettingQueryHandler(
            _loggerMock.Object,
            _permissionHelperMock.Object,
            _loaderMock.Object,
            providers);

        var query = new GetSettingQuery("GeneralSettings:RegionalSettings", "GeneralSettings");

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        var resultOption = result.Children.OfType<Option>().FirstOrDefault();
        Assert.NotNull(resultOption);
        Assert.NotEmpty(resultOption.Choices);
        Assert.Contains(resultOption.Choices, c => c.Value == "UTC");
    }

    [Fact]
    public async Task Handle_ShouldLogError_WhenProviderIsNotRegistered()
    {
        // Arrange
        var providers = new List<IDropdownValueProvider>(); // Empty - no providers registered

        var option = new Option
        {
            Key = "GeneralSettings:RegionalSettings:TimeZone",
            Type = OptionType.Choice,
            Value = "UTC",
            DynamicDropdownProviderType = typeof(TimeZoneDropdownProvider),
            Choices = []
        };

        var page = new Page
        {
            Key = "GeneralSettings:RegionalSettings",
            Label = "Regional Settings",
            Children = [option]
        };

        var populatedPages = new Dictionary<string, Page>
        {
            [page.Key] = page
        };

        _loaderMock
            .Setup(x => x.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(populatedPages);

        _permissionHelperMock
            .Setup(x => x.RemoveInaccessibleChildrenAsync(It.IsAny<Page>()))
            .Returns(Task.CompletedTask);

        var handler = new GetSettingQueryHandler(
            _loggerMock.Object,
            _permissionHelperMock.Object,
            _loaderMock.Object,
            providers);

        var query = new GetSettingQuery("GeneralSettings:RegionalSettings", "GeneralSettings");

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert - Error is logged, choices remain empty
        Assert.NotNull(result);
        var resultOption = result.Children.OfType<Option>().FirstOrDefault();
        Assert.NotNull(resultOption);
        Assert.Empty(resultOption.Choices); // Choices should remain empty due to error

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotPopulateChoices_WhenNoDynamicDropdownProviderType()
    {
        // Arrange
        var timeZoneProvider = new TimeZoneDropdownProvider();
        var providers = new List<IDropdownValueProvider> { timeZoneProvider };

        var option = new Option
        {
            Key = "SomeSettings:SomeOption",
            Type = OptionType.String,
            Value = "Some Value",
            DynamicDropdownProviderType = null, // No dynamic dropdown
            Choices = []
        };

        var page = new Page
        {
            Key = "SomeSettings",
            Label = "Some Settings",
            Children = [option]
        };

        var populatedPages = new Dictionary<string, Page>
        {
            [page.Key] = page
        };

        _loaderMock
            .Setup(x => x.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(populatedPages);

        _permissionHelperMock
            .Setup(x => x.RemoveInaccessibleChildrenAsync(It.IsAny<Page>()))
            .Returns(Task.CompletedTask);

        var handler = new GetSettingQueryHandler(
            _loggerMock.Object,
            _permissionHelperMock.Object,
            _loaderMock.Object,
            providers);

        var query = new GetSettingQuery("SomeSettings", "SomeSettings");

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        var resultOption = result.Children.OfType<Option>().FirstOrDefault();
        Assert.NotNull(resultOption);
        Assert.Empty(resultOption.Choices); // Should remain empty
    }

    [Fact]
    public async Task Handle_ShouldPopulateNestedDynamicDropdowns()
    {
        // Arrange
        var timeZoneProvider = new TimeZoneDropdownProvider();
        var providers = new List<IDropdownValueProvider> { timeZoneProvider };

        var nestedOption = new Option
        {
            Key = "Parent:Child:TimeZone",
            Type = OptionType.Choice,
            Value = "UTC",
            DynamicDropdownProviderType = typeof(TimeZoneDropdownProvider),
            Choices = []
        };

        var group = new Group
        {
            Key = "Parent:Child",
            Label = "Child Group",
            Children = [nestedOption]
        };

        var page = new Page
        {
            Key = "Parent",
            Label = "Parent Page",
            Children = [group]
        };

        var populatedPages = new Dictionary<string, Page>
        {
            [page.Key] = page
        };

        _loaderMock
            .Setup(x => x.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(populatedPages);

        _permissionHelperMock
            .Setup(x => x.RemoveInaccessibleChildrenAsync(It.IsAny<Page>()))
            .Returns(Task.CompletedTask);

        var handler = new GetSettingQueryHandler(
            _loggerMock.Object,
            _permissionHelperMock.Object,
            _loaderMock.Object,
            providers);

        var query = new GetSettingQuery("Parent", "Parent");

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        var resultGroup = result.Children.OfType<Group>().FirstOrDefault();
        Assert.NotNull(resultGroup);
        var resultOption = resultGroup.Children.OfType<Option>().FirstOrDefault();
        Assert.NotNull(resultOption);
        Assert.NotEmpty(resultOption.Choices);
        Assert.Contains(resultOption.Choices, c => c.Value == "UTC");
    }
}
