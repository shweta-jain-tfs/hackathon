using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Services;
public class PopulatorFactoryTests
{
    [Fact]
    public void GetPopulator_ReturnsPagePopulator_ForPageComponent()
    {
        // Arrange
        var component = new Page();

        // Act
        var result = PopulatorFactory.GetPopulator(component);

        // Assert
        result.Should().BeOfType<PagePopulator>();
    }

    [Fact]
    public void GetPopulator_ReturnsGroupPopulator_ForGroupComponent()
    {
        // Arrange
        var component = new Group();

        // Act
        var result = PopulatorFactory.GetPopulator(component);

        // Assert
        result.Should().BeOfType<GroupPopulator>();
    }

    [Fact]
    public void GetPopulator_ReturnsOptionPopulator_ForOptionComponent()
    {
        // Arrange
        var component = new Option();

        // Act
        var result = PopulatorFactory.GetPopulator(component);

        // Assert
        result.Should().BeOfType<OptionPopulator>();
    }

    [Fact]
    public void GetPopulator_ReturnsPageListPopulator_ForPageListComponent()
    {
        // Arrange
        var component = new PageList();

        // Act
        var result = PopulatorFactory.GetPopulator(component);

        // Assert
        result.Should().BeOfType<PageListPopulator>();
    }

    [Fact]
    public void GetPopulator_ReturnsNull_ForUnknownComponent()
    {
        // Arrange
        var component = new UnknownComponent();

        // Act
        var result = PopulatorFactory.GetPopulator(component);

        // Assert
        result.Should().BeNull();
    }

    // Mock class for an unknown component
    private class UnknownComponent : SettingItemBase
    {
    }
}
