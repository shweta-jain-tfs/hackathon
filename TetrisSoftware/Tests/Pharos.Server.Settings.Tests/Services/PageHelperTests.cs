using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Services;
public class PageHelperTests
{
    [Theory]
    [InlineData(1, 1, 1, 1)]
    [InlineData(1, 1, 0, 0)]
    [InlineData(1, 1, 2, 2)]
    [InlineData(1, 2, 2, 4)]
    [InlineData(5, 3, 2, 30)]
    public void FindOptions_ShouldReturnAllOptionsInPage(int numGroups, int numPagePerGroup, int numOptionsPerGroup, int expectedNumOptions)
    {
        var page = GeneratePage(numGroups, numPagePerGroup, numOptionsPerGroup);
        var options = PageHelper.FindOptions(page);
        Assert.NotNull(options);
        Assert.Equal(expectedNumOptions, options.Count);
    }

    private static Page GeneratePage(int numGroups, int numPagePerGroup, int numOptionsPerGroup)
    {
        var rootPage = new Page();

        for (var i = 0; i < numGroups; i++)
        {
            var group = new Group();
            for (var j = 0; j < numPagePerGroup; j++)
            {
                var page = new Page();
                for (var k = 0; k < numOptionsPerGroup; k++)
                {
                    var option = new Option()
                    {
                        Key = $"key_{i}_{j}_{k}",
                        Value = $"value_{i}_{j}_{k}"
                    };
                    page.Children.Add(option);
                }
                group.Children.Add(page);
            }
            rootPage.Children.Add(group);
        }

        return rootPage;
    }

    [Fact]
    public void IsHidden_ShouldReturnFalse_WhenSettingHasNoDependency()
    {
        // Arrange
        var page = new Page();
        var setting = new Option { Dependency = [] };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsHidden_ShouldReturnFalse_WhenSettingHasEmptyDependency()
    {
        // Arrange
        var page = new Page();
        var setting = new Option { Dependency = [] };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsHidden_ShouldReturnFalse_WhenDependencyTypeIsNotHide()
    {
        // Arrange
        var page = new Page();
        var setting = new Option
        {
            Dependency = new List<Dependency>
                {
                    new() { Type = DependencyType.Disable, MainOptionKey = "key", MainOptionValue = "value" }
                }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsHidden_ShouldReturnFalse_WhenDependencyOptionValueDoesMatch()
    {
        // Arrange
        var page = new Page
        {
            Key = "page",
            Children = new List<SettingsChildItem>
                {
                    new Option { Key = "page:option", Value = "value" }
                }
        };
        var setting = new Option
        {
            Dependency = new List<Dependency>
                {
                    new() { Type = DependencyType.Hide, MainOptionKey = "page:option", MainOptionValue = "value" }
                }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsHidden_ShouldReturnTrue_WhenDependencyOptionValueDoesNotMatch()
    {
        // Arrange
        var page = new Page
        {
            Key = "page",
            Children = new List<SettingsChildItem>
                {
                    new Option { Key = "page:option", Value = "wrongValue" }
                }
        };
        var setting = new Option
        {
            Dependency = new List<Dependency>
                {
                    new() { Type = DependencyType.Hide, MainOptionKey = "page:option", MainOptionValue = "value" }
                }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsHidden_ShouldReturnTrue_WhenSettingIsGroupAndDependencyOptionValueDoesNotMatch()
    {
        // Arrange
        var page = new Page
        {
            Key = "page",
            Children = new List<SettingsChildItem>
                {
                    new Option { Key = "page:key", Value = "wrongValue" }
                }
        };
        var setting = new Group
        {
            Dependency = new List<Dependency>
                {
                    new() { Type = DependencyType.Hide, MainOptionKey = "page:key", MainOptionValue = "value" }
                }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsHidden_ShouldReturnFalse_WhenSettingIsGroupAndDependencyOptionValueDoesMatch()
    {
        // Arrange
        var page = new Page
        {
            Key = "page",
            Children = new List<SettingsChildItem>
                {
                    new Option { Key = "page:key", Value = "value" }
                }
        };
        var setting = new Group
        {
            Dependency = new List<Dependency>
                {
                    new() { Type = DependencyType.Hide, MainOptionKey = "page:key", MainOptionValue = "value" }
                }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsHidden_ShouldReturnTrue_WhenSettingIsPageAndDependencyOptionValueDoesNotMatch()
    {
        // Arrange
        var page = new Page
        {
            Key = "page",
            Children = new List<SettingsChildItem>
                {
                    new Option { Key = "page:key", Value = "wrongValue" }
                }
        };
        var setting = new Page
        {
            Dependency = new List<Dependency>
                {
                    new() { Type = DependencyType.Hide, MainOptionKey = "page:key", MainOptionValue = "value" }
                }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsHidden_ShouldReturnFalse_WhenSettingIsPageAndDependencyOptionValueDoesMatch()
    {
        // Arrange
        var page = new Page
        {
            Key = "page",
            Children = new List<SettingsChildItem>
                {
                    new Option { Key = "page:key", Value = "value" }
                }
        };
        var setting = new Page
        {
            Dependency = new List<Dependency>
                {
                    new() { Type = DependencyType.Hide, MainOptionKey = "page:key", MainOptionValue = "value" }
                }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsHidden_ShouldReturnTrue_WhenAnyDependencyOptionValueDoesNotMatch()
    {
        // Arrange
        var page = new Page
        {
            Key = "page",
            Children = new List<SettingsChildItem>
                {
                    new Option { Key = "page:key1", Value = "value1" },
                    new Option { Key = "page:key2", Value = "wrongValue" }
                }
        };
        var setting = new Option
        {
            Dependency = new List<Dependency>
                {
                    new() { Type = DependencyType.Hide, MainOptionKey = "page:key1", MainOptionValue = "value1" },
                    new() { Type = DependencyType.Hide, MainOptionKey = "page:key2", MainOptionValue = "value2" }
                }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsHidden_ShouldReturnFalse_WhenAllDependencyOptionValuesMatch()
    {
        // Arrange
        var page = new Page
        {
            Key = "page",
            Children = new List<SettingsChildItem>
                {
                    new Option { Key = "page:key1", Value = "value1" },
                    new Option { Key = "page:key2", Value = "value2" }
                }
        };
        var setting = new Option
        {
            Dependency = new List<Dependency>
                {
                    new() { Type = DependencyType.Hide, MainOptionKey = "page:key1", MainOptionValue = "value1" },
                    new() { Type = DependencyType.Hide, MainOptionKey = "page:key2", MainOptionValue = "value2" }
                }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeFalse();
    }

    // OR semantics: same property listed twice with different activating values

    [Fact]
    public void IsHidden_ShouldReturnFalse_WhenControllerMatchesFirstOfMultipleActivatingValues()
    {
        // Arrange
        var page = new Page
        {
            Key = "page",
            Children = new List<SettingsChildItem>
            {
                new Option { Key = "page:key", Value = "value1" }
            }
        };
        var setting = new Option
        {
            Dependency = new List<Dependency>
            {
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key", MainOptionValue = "value1" },
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key", MainOptionValue = "value2" }
            }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsHidden_ShouldReturnFalse_WhenControllerMatchesSecondOfMultipleActivatingValues()
    {
        // Arrange
        var page = new Page
        {
            Key = "page",
            Children = new List<SettingsChildItem>
            {
                new Option { Key = "page:key", Value = "value2" }
            }
        };
        var setting = new Option
        {
            Dependency = new List<Dependency>
            {
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key", MainOptionValue = "value1" },
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key", MainOptionValue = "value2" }
            }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsHidden_ShouldReturnTrue_WhenControllerMatchesNoneOfMultipleActivatingValues()
    {
        // Arrange
        var page = new Page
        {
            Key = "page",
            Children = new List<SettingsChildItem>
            {
                new Option { Key = "page:key", Value = "wrongValue" }
            }
        };
        var setting = new Option
        {
            Dependency = new List<Dependency>
            {
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key", MainOptionValue = "value1" },
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key", MainOptionValue = "value2" }
            }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeTrue();
    }

    // AND + OR combined: two properties each with multiple activating values

    [Fact]
    public void IsHidden_ShouldReturnFalse_WhenEachControllerMatchesAnyOfItsActivatingValues()
    {
        // Arrange
        var page = new Page
        {
            Key = "page",
            Children = new List<SettingsChildItem>
            {
                new Option { Key = "page:key1", Value = "value1b" },
                new Option { Key = "page:key2", Value = "value2a" }
            }
        };
        var setting = new Option
        {
            Dependency = new List<Dependency>
            {
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key1", MainOptionValue = "value1a" },
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key1", MainOptionValue = "value1b" },
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key2", MainOptionValue = "value2a" },
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key2", MainOptionValue = "value2b" }
            }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsHidden_ShouldReturnTrue_WhenOneControllerMatchesNoneOfItsActivatingValues()
    {
        // Arrange
        var page = new Page
        {
            Key = "page",
            Children = new List<SettingsChildItem>
            {
                new Option { Key = "page:key1", Value = "value1a" },
                new Option { Key = "page:key2", Value = "wrongValue" }
            }
        };
        var setting = new Option
        {
            Dependency = new List<Dependency>
            {
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key1", MainOptionValue = "value1a" },
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key1", MainOptionValue = "value1b" },
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key2", MainOptionValue = "value2a" },
                new() { Type = DependencyType.Hide, MainOptionKey = "page:key2", MainOptionValue = "value2b" }
            }
        };

        // Act
        var result = PageHelper.IsHidden(page, setting);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void FindOptions_ShouldReturnOptions_WhenPageListContainsPagePlaceholders()
    {
        // Arrange
        var page = new Page
        {
            Key = "root",
            Children = new List<SettingsChildItem>
            {
                new PageList
                {
                    Key = "root:pageList",
                    PageEntries = new List<ChildPageItem>
                    {
                        new PagePlaceholder
                        {
                            Key = "root:pageList:0",
                            Options = new List<Option>
                            {
                                new() { Key = "root:pageList:0:option1", Value = "value1" },
                                new() { Key = "root:pageList:0:option2", Value = "value2" }
                            }
                        },
                        new PagePlaceholder
                        {
                            Key = "root:pageList:1",
                            Options = new List<Option>
                            {
                                new() { Key = "root:pageList:1:option1", Value = "value3" }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var options = PageHelper.FindOptions(page);

        // Assert
        options.Should().HaveCount(3);
        options.Should().ContainKey("root:pageList:0:option1");
        options.Should().ContainKey("root:pageList:0:option2");
        options.Should().ContainKey("root:pageList:1:option1");
    }

    [Fact]
    public void FindOptions_ShouldReturnOptions_WhenPageListContainsMixOfPagesAndPagePlaceholders()
    {
        // Arrange
        var page = new Page
        {
            Key = "root",
            Children = new List<SettingsChildItem>
            {
                new PageList
                {
                    Key = "root:pageList",
                    PageEntries = new List<ChildPageItem>
                    {
                        new Page
                        {
                            Key = "root:pageList:0",
                            Children = new List<SettingsChildItem>
                            {
                                new Option { Key = "root:pageList:0:pageOption", Value = "pageValue" }
                            }
                        },
                        new PagePlaceholder
                        {
                            Key = "root:pageList:1",
                            Options = new List<Option>
                            {
                                new() { Key = "root:pageList:1:placeholderOption", Value = "placeholderValue" }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var options = PageHelper.FindOptions(page);

        // Assert
        options.Should().HaveCount(2);
        options.Should().ContainKey("root:pageList:0:pageOption");
        options.Should().ContainKey("root:pageList:1:placeholderOption");
    }
}