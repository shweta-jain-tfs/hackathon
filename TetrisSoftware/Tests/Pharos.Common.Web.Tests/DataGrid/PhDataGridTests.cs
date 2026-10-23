using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Loader;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Models.DataGrid;
using Pharos.Common.Web.Services;

namespace Pharos.Common.Web.Tests.DataGrid;

public class PhDataGridTests : BunitContext
{
    public PhDataGridTests()
    {
        // JS interop: we don't care, so allow anything
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Mock services the grid injects (we don't assert on them, just satisfy DI)
        Services.AddSingleton(_ => new Mock<LoaderService>().Object);
        Services.AddSingleton(_ => new Mock<IModalService>().Object);
        Services.AddSingleton(_ => new Mock<IComponentStateService>().Object);
    }

    [Fact]
    public void RendersData()
    {
        var people = new[]
        {
            new Person(1, "Alice", "Anderson"),
            new Person(2, "Bob", "Brown")
        }.AsQueryable();

        var cut = Render<TestDataGrid>(parameters => parameters
            .Add(p => p.Items, people));

        // Wait for async rendering
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Alice Anderson");
            cut.Markup.Should().Contain("Bob Brown");
        });
    }

    [Fact]
    public void UpdatesSelectedItems_WhenCheckboxClicked()
    {
        var people = new[]
        {
            new Person(1, "Alice", "Anderson"),
            new Person(2, "Bob", "Brown")
        }.AsQueryable();

        var selected = new HashSet<Person>();

        var cut = Render<TestDataGrid>(parameters => parameters
            .Add(p => p.Items, people)
            .Add(p => p.OnSelectedItemsChanged, items => selected = items));

        // Wait for async rendering
        cut.WaitForAssertion(() =>
        {
            var checkboxes = cut.FindAll("tbody input[type=checkbox]");
            checkboxes.Should().NotBeEmpty();
        });

        var checkbox = cut.FindAll("tbody input[type=checkbox]")[0];
        checkbox.Change(true);

        selected.Should().ContainSingle();
        selected.First().FirstName.Should().Be("Alice");
    }

    [Fact]
    public void RendersRadioStyle_WhenSingleSelectIsConfigured()
    {
        var people = new[]
        {
            new Person(1, "Alice", "Anderson"),
            new Person(2, "Bob", "Brown")
        }.AsQueryable();

        var cut = Render<TestDataGrid>(parameters => parameters
            .Add(p => p.Items, people)
            .Add(p => p.SelectionType, SelectionTypeEnum.SingleSelect));

        cut.WaitForAssertion(() =>
        {
            var radioIndicators = cut.FindAll("tbody .ph-checkbox--radio");
            radioIndicators.Should().NotBeEmpty();
            cut.FindAll("tbody input[type=checkbox]").Should().NotBeEmpty();
        });
    }

    [Theory]
    // Each case defines whether the checkbox is shown, disabled, and whether the row is dimmed.
    [InlineData(ItemStyleFlags.CheckboxDisabled, true, true, false)]
    [InlineData(ItemStyleFlags.CheckboxHidden, false, false, false)]
    [InlineData(ItemStyleFlags.Disabled, true, true, true)]
    public void RendersRowStyle(ItemStyleFlags style, bool checkboxVisible, bool checkboxDisabled, bool rowDimmed)
    {
        var people = new[]
        {
            new Person(1, "Alice", "Anderson"),
            new Person(2, "Bob", "Brown")
        }.AsQueryable();

        var cut = Render<TestDataGrid>(parameters => parameters
            .Add(p => p.Items, people)
            .Add(p => p.ItemStyleOptions, CreateItemStyleOptions(1, style)));

        cut.WaitForAssertion(() =>
        {
            var row = cut.FindAll("tbody tr").Single(element => element.TextContent.Contains("Alice Anderson"));
            var checkbox = row.QuerySelector("input[type=checkbox]");

            (checkbox is not null).Should().Be(checkboxVisible);
            if (checkbox is not null)
            {
                checkbox.HasAttribute("disabled").Should().Be(checkboxDisabled);
            }
            row.ClassList.Contains("ph-datagrid__row--disabled").Should().Be(rowDimmed);
        });
    }

    [Fact]
    public void RendersCheckboxStyle_WhenMultiSelectIsConfigured()
    {
        var people = new[]
        {
            new Person(1, "Alice", "Anderson"),
            new Person(2, "Bob", "Brown")
        }.AsQueryable();

        var cut = Render<TestDataGrid>(parameters => parameters
            .Add(p => p.Items, people)
            .Add(p => p.SelectionType, SelectionTypeEnum.MultiSelect));

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("tbody .ph-checkbox--radio").Should().BeEmpty();
            cut.FindAll("tbody input[type=checkbox]").Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task ClearSelectedItem_ClearsSelection()
    {
        var people = new[]
        {
            new Person(1, "Alice", "Anderson"),
            new Person(2, "Bob", "Brown")
        }.AsQueryable();

        var selected = new HashSet<Person>();

        var cut = Render<TestDataGrid>(parameters => parameters
            .Add(p => p.Items, people)
            .Add(p => p.OnSelectedItemsChanged, items => selected = items));

        // Wait for async rendering
        cut.WaitForAssertion(() =>
        {
            var checkboxes = cut.FindAll("tbody input[type=checkbox]");
            checkboxes.Should().NotBeEmpty();
        });

        cut.FindAll("tbody input[type=checkbox]")[0].Change(true);
        selected.Should().ContainSingle();

        var grid = cut.FindComponent<PhDataGrid<Person>>();
        await grid.Instance.ClearSelectedItem();

        selected.Should().BeEmpty();
    }

    [Fact]
    public void ItemsProvider_CalledOnce_OnInitialRender()
    {
        var people = new[]
        {
            new Person(1, "Alice", "Anderson"),
            new Person(2, "Bob", "Brown")
        };

        var providerCallCount = 0;

        GridItemsProvider<Person> provider = _ =>
        {
            providerCallCount++;
            return ValueTask.FromResult(
                GridItemsProviderResult.From(
                    items: people,
                    totalItemCount: people.Length
                )
            );
        };

        var cut = Render<TestDataGridWithProvider>(parameters => parameters
            .Add(p => p.ItemsProvider, provider));

        // Wait for async rendering
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Alice Anderson");
            cut.Markup.Should().Contain("Bob Brown");
        });

        providerCallCount.Should().Be(1);
    }

    /// <summary>
    /// Creates row style options that match the person with the specified identifier.
    /// </summary>
    private static ItemStyleOptions<Person> CreateItemStyleOptions(int personId, ItemStyleFlags style)
    {
        return new ItemStyleOptions<Person>
        {
            Items = [personId],
            Match = static (person, item) => person.Id == (int)item,
            Style = style
        };
    }

    [Fact]
    public void ScrollToItem_ScrollsToResolvedIndex()
    {
        var people = new[]
        {
            new Person(1, "Alice", "Anderson"),
            new Person(2, "Bob", "Brown"),
            new Person(3, "Charlie", "Clark")
        };

        GridItemsProvider<Person> provider = _ =>
            ValueTask.FromResult(GridItemsProviderResult.From(people, people.Length));

        var importInterop = JSInterop.SetupModule("./_content/Pharos.Common.Web/js/PhDataGrid.js");
        var targetIndex = 2;
        var resolverCallCount = 0;

        importInterop
            .Setup<bool>("scrollToRowIndex", invocation =>
                invocation.Arguments.Count >= 2 &&
                invocation.Arguments[1] is int index &&
                index == targetIndex)
            .SetResult(true);

        Task<int> Resolver(string key)
        {
            resolverCallCount++;
            key.Should().Be("person-3");
            return Task.FromResult(targetIndex);
        }

        var cut = Render<TestDataGridWithProvider>(parameters => parameters
            .Add(p => p.ItemsProvider, provider));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Charlie Clark");
        });

        cut.Render(parameters => parameters
            .Add(p => p.ItemsProvider, provider)
            .Add(p => p.ScrollToItemKey, "person-3")
            .Add(p => p.ScrollToItemIndexResolver, Resolver));

        cut.WaitForAssertion(() =>
        {
            resolverCallCount.Should().Be(1);
            importInterop.VerifyInvoke("scrollToRowIndex");
        });
    }

    [Fact]
    public void RendersEmpty_WhenNoData()
    {
        var cut = Render<TestDataGrid>(parameters => parameters
            .Add(p => p.Items, Enumerable.Empty<Person>().AsQueryable()));

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            rows.Should().BeEmpty();
        });
    }
}
