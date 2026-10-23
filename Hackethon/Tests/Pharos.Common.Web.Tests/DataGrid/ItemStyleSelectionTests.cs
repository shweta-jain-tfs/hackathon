using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Loader;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Services;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Tests.DataGrid;

public class ItemStyleSelectionTests : BunitContext
{
    public ItemStyleSelectionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new Mock<LoaderService>().Object);
        Services.AddSingleton(new Mock<IModalService>().Object);
        Services.AddSingleton(new Mock<IComponentStateService>().Object);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PerRowStyles_ControlAppearanceAndSelection_RegardlessOfMatchingOptions(bool includeMatchingOptions)
    {
        var people = new[]
        {
            new Person(1, "Alice", "Anderson"),
            new Person(2, "Bob", "Brown"),
            new Person(3, "Charlie", "Clark"),
            new Person(4, "Diana", "Davis")
        };
        var selected = new HashSet<Person>();
        var options = new ItemStyleOptions<Person>
        {
            StyleSelector = person => person.Id switch
            {
                1 => ItemStyleFlags.Disabled,
                3 => ItemStyleFlags.CheckboxHidden,
                _ => ItemStyleFlags.None
            }
        };
        if (includeMatchingOptions)
        {
            // A selector must ignore matching rules and override even a disabled shared style.
            options = options with
            {
                Items = [1],
                Match = (_, _) => throw new InvalidOperationException("Match must not run when a selector is supplied."),
                Style = ItemStyleFlags.Disabled
            };
        }
        var cut = Render<TestDataGrid>(parameters => parameters
            .Add(p => p.Items, people.AsQueryable())
            .Add(p => p.SelectionType, SelectionTypeEnum.MultiSelectWithSelectAll)
            .Add(p => p.ItemStyleOptions, options)
            .Add(p => p.OnSelectedItemsChanged, items => selected = items));

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            var alice = rows.Single(row => row.TextContent.Contains("Alice Anderson"));
            alice.ClassList.Should().Contain("ph-datagrid__row--disabled");
            alice.QuerySelector("input[type=checkbox]")!.HasAttribute("disabled").Should().BeTrue();

            var bob = rows.Single(row => row.TextContent.Contains("Bob Brown"));
            bob.ClassList.Should().NotContain("ph-datagrid__row--disabled");
            bob.QuerySelector("input[type=checkbox]")!.HasAttribute("disabled").Should().BeFalse();

            var charlie = rows.Single(row => row.TextContent.Contains("Charlie Clark"));
            charlie.ClassList.Should().NotContain("ph-datagrid__row--disabled");
            charlie.QuerySelector("input[type=checkbox]").Should().BeNull();

            var diana = rows.Single(row => row.TextContent.Contains("Diana Davis"));
            diana.ClassList.Should().NotContain("ph-datagrid__row--disabled");
            diana.QuerySelector("input[type=checkbox]")!.HasAttribute("disabled").Should().BeFalse();
        });

        cut.Find("thead input[type=checkbox]").Change(true);
        cut.WaitForAssertion(() =>
        {
            selected.Should().BeEquivalentTo(new[] { people[1], people[3] });
            cut.Find("thead input[type=checkbox]").GetAttribute("aria-checked").Should().Be("mixed");
        });
        cut.Find("thead input[type=checkbox]").Change(false);
        cut.WaitForAssertion(() => selected.Should().BeEmpty());

        var grid = cut.FindComponent<PhDataGrid<Person>>();
        await cut.InvokeAsync(() => grid.Instance.SetSelectItems([.. people]));
        cut.WaitForAssertion(() => selected.Should().BeEquivalentTo(new[] { people[1], people[3] }));
    }

    [Theory]
    // True means the styled row can be selected; false means it must be skipped.
    [InlineData(ItemStyleFlags.None, true)]
    [InlineData(ItemStyleFlags.Checked, true)]
    [InlineData(ItemStyleFlags.Dimmed, true)]
    [InlineData(ItemStyleFlags.Disabled, false)]
    [InlineData(ItemStyleFlags.CheckboxDisabled, false)]
    [InlineData(ItemStyleFlags.CheckboxHidden, false)]
    [InlineData(ItemStyleFlags.Dimmed | ItemStyleFlags.Checked, true)]
    [InlineData(ItemStyleFlags.Disabled | ItemStyleFlags.Checked, false)]
    [InlineData(ItemStyleFlags.CheckboxDisabled | ItemStyleFlags.Checked, false)]
    [InlineData(ItemStyleFlags.CheckboxHidden | ItemStyleFlags.Checked, false)]
    [InlineData(ItemStyleFlags.Dimmed | ItemStyleFlags.CheckboxHidden, false)]
    public async Task SelectAllAndProgrammaticSelection_RespectRowStyles(ItemStyleFlags flags, bool canSelectStyledRow)
    {
        var people = new[] { new Person(1, "Alice", "Anderson"), new Person(2, "Bob", "Brown") };
        var selected = new HashSet<Person>();
        var options = new ItemStyleOptions<Person>
        {
            Items = [1],
            Match = (person, item) => person.Id == (int)item,
            Style = flags
        };
        var cut = Render<TestDataGrid>(parameters => parameters
            .Add(p => p.Items, people.AsQueryable())
            .Add(p => p.SelectionType, SelectionTypeEnum.MultiSelectWithSelectAll)
            .Add(p => p.ItemStyleOptions, options)
            .Add(p => p.OnSelectedItemsChanged, items => selected = items));
        cut.WaitForAssertion(() => cut.FindAll("tbody tr").Should().HaveCount(2));

        // Bob has no special style and is always selectable; Alice depends on the test case.
        var expected = canSelectStyledRow ? people : new[] { people[1] };
        cut.Find("thead input[type=checkbox]").Change(true);
        cut.WaitForAssertion(() =>
        {
            selected.Should().BeEquivalentTo(expected);
            cut.Find("thead input[type=checkbox]").GetAttribute("aria-checked")
                .Should().Be(canSelectStyledRow ? "true" : "mixed");
        });

        cut.Find("thead input[type=checkbox]").Change(false);
        cut.WaitForAssertion(() =>
        {
            selected.Should().BeEmpty();
            cut.Find("thead input[type=checkbox]").GetAttribute("aria-checked").Should().Be("false");
        });

        var grid = cut.FindComponent<PhDataGrid<Person>>();
        await cut.InvokeAsync(() => grid.Instance.SetSelectItems([.. people]));
        cut.WaitForAssertion(() => selected.Should().BeEquivalentTo(expected));
    }

    [Theory]
    [InlineData(ItemStyleFlags.Disabled)]
    [InlineData(ItemStyleFlags.CheckboxDisabled)]
    [InlineData(ItemStyleFlags.CheckboxHidden)]
    public void ChangingFlags_RemovesPreviouslySelectedRows(ItemStyleFlags flags)
    {
        var people = new[] { new Person(1, "Alice", "Anderson"), new Person(2, "Bob", "Brown") };
        var selected = new HashSet<Person>();
        var cut = Render<TestDataGrid>(parameters => parameters
            .Add(p => p.Items, people.AsQueryable())
            .Add(p => p.SelectionType, SelectionTypeEnum.MultiSelectWithSelectAll)
            .Add(p => p.OnSelectedItemsChanged, items => selected = items));
        cut.WaitForAssertion(() => cut.FindAll("tbody tr").Should().HaveCount(2));
        cut.Find("thead input[type=checkbox]").Change(true);
        cut.WaitForAssertion(() => selected.Should().HaveCount(2));

        cut.Render(parameters => parameters.Add(p => p.ItemStyleOptions, new ItemStyleOptions<Person>
        {
            Items = [1],
            Match = (person, item) => person.Id == (int)item,
            Style = flags
        }));
        cut.WaitForAssertion(() => selected.Should().ContainSingle().Which.Id.Should().Be(2));
    }

    [Fact]
    public void ReplacingItemsAndDisablingSelectedRow_RefreshesDataAndRemovesSelection()
    {
        var people = new[] { new Person(1, "Alice", "Anderson"), new Person(2, "Bob", "Brown") };
        var selected = new HashSet<Person>();
        var cut = Render<TestDataGrid>(parameters => parameters
            .Add(p => p.Items, people.AsQueryable())
            .Add(p => p.OnSelectedItemsChanged, items => selected = items));
        cut.WaitForAssertion(() => cut.FindAll("tbody tr").Should().HaveCount(2));
        cut.FindAll("tbody input[type=checkbox]")[0].Change(true);
        cut.WaitForAssertion(() => selected.Should().ContainSingle().Which.Id.Should().Be(1));

        // Disabling Alice must remove her selection and still display her updated name.
        var updatedPeople = new[] { people[0] with { LastName = "Updated" }, people[1] };
        cut.Render(parameters => parameters
            .Add(p => p.Items, updatedPeople.AsQueryable())
            .Add(p => p.ItemStyleOptions, new ItemStyleOptions<Person>
            {
                Items = [1],
                Match = (person, item) => person.Id == (int)item,
                Style = ItemStyleFlags.CheckboxDisabled
            }));

        cut.WaitForAssertion(() =>
        {
            selected.Should().BeEmpty();
            cut.Markup.Should().Contain("Alice Updated").And.NotContain("Alice Anderson");
        });
    }

    [Fact]
    public async Task ProviderSelectAll_UpdatesHeaderAfterAsyncCompletion_WithoutSelectionCallback()
    {
        var people = new[] { new Person(1, "Alice", "Anderson"), new Person(2, "Bob", "Brown") };
        var releaseSelectAll = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        GridItemsProvider<Person> provider = async request =>
        {
            if (request.RequiresRowLimitBypass)
            {
                await releaseSelectAll.Task;
            }
            return GridItemsProviderResult.From(people.Skip(request.StartIndex).Take(request.Count!.Value).ToArray(), people.Length);
        };
        var cut = Render<TestDataGridWithProvider>(parameters => parameters
            .Add(p => p.ItemsProvider, provider)
            .Add(p => p.SelectionType, SelectionTypeEnum.MultiSelectWithSelectAll)
            .Add(p => p.ItemStyleOptions, new ItemStyleOptions<Person>
            {
                Items = [1],
                Match = (person, item) => person.Id == (int)item,
                Style = ItemStyleFlags.CheckboxDisabled
            }));
        cut.WaitForAssertion(() => cut.FindAll("tbody tr").Should().HaveCount(2));
        cut.Find("thead input[type=checkbox]").Change(true);
        await cut.InvokeAsync(() => releaseSelectAll.SetResult(true));

        cut.WaitForAssertion(() =>
        {
            cut.Find("thead input[type=checkbox]").GetAttribute("aria-checked").Should().Be("mixed");
            var checkboxes = cut.FindAll("tbody input[type=checkbox]");
            checkboxes[0].HasAttribute("checked").Should().BeFalse();
            checkboxes[1].HasAttribute("checked").Should().BeTrue();
        });
    }

    [Fact]
    public void SelectAll_WhenAllRowsBlocked_LeavesSelectionEmpty()
    {
        var cut = Render<TestDataGrid>(parameters => parameters
            .Add(p => p.Items, new[] { new Person(1, "Alice", "Anderson") }.AsQueryable())
            .Add(p => p.SelectionType, SelectionTypeEnum.MultiSelectWithSelectAll)
            .Add(p => p.ItemStyleOptions, new ItemStyleOptions<Person>
            {
                Items = [1],
                Match = (_, _) => true,
                Style = ItemStyleFlags.CheckboxHidden
            }));
        cut.WaitForAssertion(() => cut.FindAll("tbody tr").Should().ContainSingle());
        cut.Find("thead input[type=checkbox]").Change(true);
        cut.WaitForAssertion(() => cut.Find("thead input[type=checkbox]").GetAttribute("aria-checked").Should().Be("false"));
    }
}
