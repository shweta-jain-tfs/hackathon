using Moq;
using Pharos.Common.Web.Components.DataGrid.Selection;
using Pharos.Common.Web.Components.Loader;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Tests.DataGrid;

public class SelectionManagerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Cleanup_RemovesMultipleRows_AndNotifiesOnceWithFinalSelection(bool useKeys)
    {
        var allowAll = true;
        using var sut = new SelectionManager<ItemWithId>(new Mock<LoaderService>().Object,
            item => allowAll || item.Name == "Allowed");
        var first = new ItemWithId { Id = useKeys ? 1 : 0, Name = "Blocked A" };
        var second = new ItemWithId { Id = useKeys ? 2 : 0, Name = "Blocked B" };
        var allowed = new ItemWithId { Id = useKeys ? 3 : 0, Name = "Allowed" };
        sut.SetSelectedItems([first, second, allowed]);
        var notifications = new List<IReadOnlyList<ItemWithId>>();
        sut.OnSelectionChanged += () => notifications.Add(sut.GetSelectedItems());
        allowAll = false;

        sut.RemoveUnselectableItems([first, second, first, allowed]);

        sut.GetSelectedItems().Should().ContainSingle().Which.Should().BeSameAs(allowed);
        notifications.Should().ContainSingle().Which.Should().ContainSingle().Which.Should().BeSameAs(allowed);
    }

    [Fact]
    public void Cleanup_WhenNothingIsRemoved_DoesNotNotify()
    {
        using var sut = new SelectionManager<ItemWithId>(new Mock<LoaderService>().Object, item => item.Name == "Allowed");
        var allowed = new ItemWithId { Id = 1, Name = "Allowed" };
        sut.SelectItem(allowed, false);
        var notificationCount = 0;
        sut.OnSelectionChanged += () => notificationCount++;

        sut.RemoveUnselectableItems([allowed, new ItemWithId { Id = 2, Name = "Blocked" }]);

        notificationCount.Should().Be(0);
        sut.GetSelectedItems().Should().ContainSingle().Which.Should().BeSameAs(allowed);
    }

    [Fact]
    public async Task SelectAllFromServer_SkipsBlockedItemsAcrossBatches_AndPreservesFilters()
    {
        using var sut = new SelectionManager<ItemWithId>(new Mock<LoaderService>().Object, item => item.Id % 2 == 0);
        var items = Enumerable.Range(1, 5002).Select(id => new ItemWithId { Id = id }).ToArray();
        var filters = new List<IColumnFilter> { new Mock<IColumnFilter>().Object };
        var offsets = new List<int>();
        GridItemsProvider<ItemWithId> provider = request =>
        {
            offsets.Add(request.StartIndex);
            request.RequiresRowLimitBypass.Should().BeTrue();
            request.SortByColumn.Should().Be(nameof(ItemWithId.Id));
            request.SortByAscending.Should().BeFalse();
            request.ColumnFilters.Should().BeEquivalentTo(filters);
            return ValueTask.FromResult(GridItemsProviderResult.From(
                items.Skip(request.StartIndex).Take(request.Count!.Value).ToArray(), items.Length));
        };

        await sut.SelectAllFromServerAsync(provider, items.Length, nameof(ItemWithId.Id), false, filters, null);

        offsets.Should().Equal(0, 5000);
        sut.GetSelectedItems().Should().HaveCount(2501).And.OnlyContain(item => item.Id % 2 == 0);
        sut.IsPartiallySelected(items.Length).Should().BeTrue();
        sut.ClearSelection();
        sut.SelectedCount.Should().Be(0);
    }

    [Fact]
    public void ReloadedBlockedRow_RemovesSelectionByKey()
    {
        using var sut = new SelectionManager<ItemWithId>(new Mock<LoaderService>().Object, item => item.Name != "Blocked");
        sut.SelectItem(new ItemWithId { Id = 1, Name = "Allowed" }, false);

        sut.RemoveUnselectableItems([new ItemWithId { Id = 1, Name = "Blocked" }]);

        sut.GetSelectedItems().Should().BeEmpty();
    }

    [Fact]
    public void SelectingBlockedItem_DoesNotReplaceExistingSingleSelection()
    {
        using var sut = new SelectionManager<ItemWithId>(new Mock<LoaderService>().Object, item => item.Id == 1);
        sut.SelectItem(new ItemWithId { Id = 1 }, true);
        sut.SelectItem(new ItemWithId { Id = 2 }, true);

        sut.GetSelectedItems().Should().ContainSingle().Which.Id.Should().Be(1);
    }

    private sealed class ItemWithId
    {
        public int Id { get; init; }
        public string? Name { get; init; }
    }

    private sealed class ItemWithSelectionKey
    {
        [SelectionKey]
        public Guid Key { get; init; }

        public string? Name { get; init; }
    }

    [Fact]
    public void SelectItem_WithSameId_DoesNotAddDuplicate()
    {
        using var sut = new SelectionManager<ItemWithId>(new Mock<LoaderService>().Object);

        sut.SelectItem(new ItemWithId { Id = 1, Name = "A" }, false);
        sut.SelectItem(new ItemWithId { Id = 1, Name = "B" }, false);

        sut.SelectedCount.Should().Be(1);
    }

    [Fact]
    public void IsItemSelected_WithSameIdDifferentInstance_ReturnsTrue()
    {
        using var sut = new SelectionManager<ItemWithId>(new Mock<LoaderService>().Object);

        sut.SelectItem(new ItemWithId { Id = 1, Name = "A" }, false);

        sut.IsItemSelected(new ItemWithId { Id = 1, Name = "B" }).Should().BeTrue();
    }

    [Fact]
    public void DeselectItem_WithSameIdDifferentInstance_RemovesSelection()
    {
        using var sut = new SelectionManager<ItemWithId>(new Mock<LoaderService>().Object);

        sut.SelectItem(new ItemWithId { Id = 1, Name = "A" }, false);

        sut.DeselectItem(new ItemWithId { Id = 1, Name = "B" });

        sut.SelectedCount.Should().Be(0);
    }

    [Fact]
    public void SelectItem_InSingleSelectMode_ReplacesPreviousSelection()
    {
        using var sut = new SelectionManager<ItemWithId>(new Mock<LoaderService>().Object);

        sut.SelectItem(new ItemWithId { Id = 1 }, true);
        sut.SelectItem(new ItemWithId { Id = 2 }, true);

        sut.SelectedCount.Should().Be(1);
        sut.IsItemSelected(new ItemWithId { Id = 1 }).Should().BeFalse();
        sut.IsItemSelected(new ItemWithId { Id = 2 }).Should().BeTrue();
    }

    [Fact]
    public void SelectItem_WithSelectionKeyAttribute_DoesNotAddDuplicate()
    {
        using var sut = new SelectionManager<ItemWithSelectionKey>(new Mock<LoaderService>().Object);

        var key = Guid.NewGuid();

        sut.SelectItem(new ItemWithSelectionKey { Key = key, Name = "A" }, false);
        sut.SelectItem(new ItemWithSelectionKey { Key = key, Name = "B" }, false);

        sut.SelectedCount.Should().Be(1);
    }

    [Fact]
    public void SelectItem_WithDefaultId_IsTreatedAsKeyless()
    {
        using var sut = new SelectionManager<ItemWithId>(new Mock<LoaderService>().Object);

        sut.SelectItem(new ItemWithId { Id = 1, Name = "Saved" }, false);

        // Items with default Id (e.g. unsaved rows) are treated as keyless
        sut.SelectItem(new ItemWithId { Id = 0, Name = "Unsaved A" }, false);
        sut.SelectItem(new ItemWithId { Id = 0, Name = "Unsaved B" }, false);

        sut.SelectedCount.Should().Be(3);
    }
}
