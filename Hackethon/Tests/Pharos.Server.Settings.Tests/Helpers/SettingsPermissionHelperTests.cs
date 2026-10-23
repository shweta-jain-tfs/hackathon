using Pharos.Server.Application.Security;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Helpers;
public class SettingsPermissionHelperTests
{
    private readonly Mock<IAuthorizationService> _mockAuthService = new();
    private SettingsPermissionHelper Helper => new(_mockAuthService.Object);


    [Fact]
    public async Task HasPermissionAsync_ReturnsTrue_IfNoPermissionSpecified()
    {
        // Arrange
        var item = new Option
        {
            Permissions = []
        };

        // Act
        var result = await Helper.HasPermissionAsync(item);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasPermissionAsync_Delegates_To_AuthService()
    {
        // Arrange
        _mockAuthService.Setup(s => s.AuthorizeCurrentUserAsync("Permitted")).ReturnsAsync(true);
        var item = new Option
        {
            Permissions = ["Permitted"]
        };

        // Act
        var result = await Helper.HasPermissionAsync(item);

        // Assert
        Assert.True(result);
        _mockAuthService.Verify(s => s.AuthorizeCurrentUserAsync("Permitted"), Times.Once);
    }

    [Fact]
    public async Task HasPermissionAsync_TwoPermissions_ReturnsTrue_WhenUserHasBoth()
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.AuthorizeCurrentUserAsync("PermittedA", "PermittedB"))
            .ReturnsAsync(true);

        var item = new Option
        {
            Permissions = ["PermittedA", "PermittedB"]
        };

        // Act
        var result = await Helper.HasPermissionAsync(item);

        // Assert
        Assert.True(result);
        _mockAuthService.Verify(
            s => s.AuthorizeCurrentUserAsync("PermittedA", "PermittedB"),
            Times.Once);
    }

    [Theory]
    [InlineData(true, false)]   // user has only A
    [InlineData(false, true)]   // user has only B
    [InlineData(false, false)]  // user has neither
    public async Task HasPermissionAsync_TwoPermissions_ReturnsFalse_WhenUserDoesNotHaveBoth(bool hasA, bool hasB)
    {
        // Arrange
        _mockAuthService
            .Setup(s => s.AuthorizeCurrentUserAsync("PermittedA", "PermittedB"))
            .ReturnsAsync(hasA && hasB);

        var item = new Option
        {
            Permissions = ["PermittedA", "PermittedB"]
        };

        // Act
        var result = await Helper.HasPermissionAsync(item);

        // Assert
        Assert.Equal(hasA && hasB, result);
        _mockAuthService.Verify(
            s => s.AuthorizeCurrentUserAsync("PermittedA", "PermittedB"),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(RemoveInaccessibleChildrenTestData))]
    public async Task RemoveInaccessibleChildrenAsync_Removes_Unauthorized_Children(Page parent, string[] expectedPermission)
    {
        // Arrange
        _mockAuthService.Setup(s => s.AuthorizeCurrentUserAsync("Permitted")).ReturnsAsync(true);
        _mockAuthService.Setup(s => s.AuthorizeCurrentUserAsync("NotPermitted")).ReturnsAsync(false);

        // Act
        await Helper.RemoveInaccessibleChildrenAsync(parent);

        // Assert
        foreach (var child in parent.Children)
        {
            switch (child)
            {
                case Option childOption:
                    Assert.All(expectedPermission, p =>
                            Assert.Contains(p, childOption.Permissions));
                    break;
                case PageList pageList:
                    {
                        // Check PageStructure children
                        if (pageList.PageStructure is Page pageStructure)
                        {
                            foreach (var option in pageStructure.Children.OfType<Option>())
                            {
                                Assert.All(expectedPermission, p =>
                                        Assert.Contains(p, option.Permissions));
                            }
                        }

                        // Check PageEntries children
                        foreach (var entry in pageList.PageEntries.OfType<Page>())
                        {
                            foreach (var option in entry.Children.OfType<Option>())
                            {
                                Assert.All(expectedPermission, p =>
                                        Assert.Contains(p, option.Permissions));
                            }
                        }
                        break;
                    }
            }
        }
    }

    public static IEnumerable<object[]> RemoveInaccessibleChildrenTestData()
    {
        // Case 1: Only permitted child remains
        yield return
        [
            new Page
            {
                Children =
                [
                    new Option
                    {
                        Permissions = ["Permitted"]
                    },
                    new Option
                    {
                        Permissions = ["NotPermitted"]
                    }
                ]
            },
            new[] { "Permitted" }
        ];

        // Case 2: All children permitted
        yield return
        [
            new Page
            {
                Children =
                [
                    new Option
                    {
                        Permissions = ["Permitted"]
                    },
                    new Option
                    {
                        Permissions = ["Permitted"]
                    }
                ]
            },
            new[] { "Permitted", "Permitted" }
        ];

        // Case 3: All children not permitted
        yield return
        [
            new Page
            {
                Children =
                [
                    new Option
                    {
                        Permissions = ["NotPermitted"]
                    },
                    new Option
                    {
                        Permissions = ["NotPermitted"]
                    }
                ]
            },
            Array.Empty<string>()
        ];

        // Case 4: PageList with permitted and not permitted entries
        yield return
        [
            new Page
            {
                Children =
                [
                    new PageList
                    {
                        PageStructure = new Page
                        {
                            Children =
                            [
                                new Option
                                {
                                    Permissions = ["Permitted"]
                                },

                                new Option
                                {
                                    Permissions = ["NotPermitted"]
                                }
                            ]
                        },
                        PageEntries =
                        [
                            new Page
                            {
                                Children =
                                [
                                    new Option
                                    {
                                        Permissions = ["Permitted"]
                                    }
                                ]
                            },

                            new Page
                            {
                                Children =
                                [
                                    new Option
                                    {
                                        Permissions = ["NotPermitted"]
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            new[] { "Permitted" }
        ];
    }

    [Fact]
    public async Task RemoveInaccessibleChildrenAsync_Rebuilds_PageListColumns_After_Pruning()
    {
        // Arrange
        _mockAuthService.Setup(s => s.AuthorizeCurrentUserAsync("Permitted")).ReturnsAsync(true);
        _mockAuthService.Setup(s => s.AuthorizeCurrentUserAsync("NotPermitted")).ReturnsAsync(false);

        var pageList = new PageList
        {
            Key = "Root:Entries",
            PageStructure = new Page
            {
                Key = "Root:Entries",
                Children =
                [
                    new Option
                    {
                        Key = "Root:Entries:Name",
                        Label = "Name",
                        IsNameColumn = true
                    },
                    new Group
                    {
                        Key = "Root:Entries:Details",
                        Children =
                        [
                            new Option
                            {
                                Key = "Root:Entries:Details:Allowed",
                                Label = "Allowed",
                                IsColumnHeader = true,
                                Permissions = ["Permitted"]
                            },
                            new Option
                            {
                                Key = "Root:Entries:Details:Denied",
                                Label = "Denied",
                                IsColumnHeader = true,
                                Permissions = ["NotPermitted"]
                            }
                        ]
                    }
                ]
            },
            PageEntries =
            [
                new Page
                {
                    Key = "Root:Entries:0",
                    Children =
                    [
                        new Option
                        {
                            Key = "Root:Entries:0:Name",
                            Label = "Name",
                            Value = "Entry",
                            IsNameColumn = true
                        },
                        new Group
                        {
                            Key = "Root:Entries:0:Details",
                            Children =
                            [
                                new Option
                                {
                                    Key = "Root:Entries:0:Details:Allowed",
                                    Label = "Allowed",
                                    Value = "Visible",
                                    IsColumnHeader = true,
                                    Permissions = ["Permitted"]
                                },
                                new Option
                                {
                                    Key = "Root:Entries:0:Details:Denied",
                                    Label = "Denied",
                                    Value = "Hidden",
                                    IsColumnHeader = true,
                                    Permissions = ["NotPermitted"]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        PageListColumnHelper.Rebuild(pageList);
        pageList.ColumnHeaderLabels.Should().Equal("Name", "Allowed", "Denied");

        var parent = new Page
        {
            Children = [pageList]
        };

        // Act
        await Helper.RemoveInaccessibleChildrenAsync(parent);

        // Assert
        pageList.ColumnHeaderLabels.Should().Equal("Name", "Allowed");
        pageList.Columns.Select(c => c.ColumnHeaderLabel).Should().Equal("Name", "Allowed");
        pageList.Columns.Select(c => c.Value!.Value).Should().Equal("Entry", "Visible");
    }

    [Theory]
    [InlineData(typeof(Option), true)]
    [InlineData(typeof(Option), false)]
    [InlineData(typeof(Group), true)]
    [InlineData(typeof(Group), false)]
    [InlineData(typeof(Page), true)]
    [InlineData(typeof(Page), false)]
    [InlineData(typeof(PageList), true)]
    [InlineData(typeof(PageList), false)]
    [InlineData(typeof(ActionButton), true)]
    [InlineData(typeof(ActionButton), false)]
    public async Task HasPermissionAsync_WithShowOnlyInRndMode_ReturnsExpected_BasedOnRndMode(
    Type itemType,
    bool rndModeActive)
    {
        // Arrange
        _mockAuthService.Setup(s => s.RndMode).Returns(rndModeActive);

        var item = (SettingItemBase)Activator.CreateInstance(itemType)!;
        item.ShowOnlyInRndMode = true;

        // Act
        var result = await Helper.HasPermissionAsync(item);

        // Assert
        Assert.Equal(rndModeActive, result);
    }
}
