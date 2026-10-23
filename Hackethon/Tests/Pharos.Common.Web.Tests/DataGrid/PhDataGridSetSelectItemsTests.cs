using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Services;

namespace Pharos.Common.Web.Tests.DataGrid;

public sealed class PhDataGridSetSelectItemsTests : BunitContext
{
    private sealed record TestItem(int Id);

    public PhDataGridSetSelectItemsTests()
    {
        // JS interop: we don't care, so allow anything
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Component DI requirements
        Services.AddSingleton(new Mock<Components.Loader.LoaderService>().Object);
        Services.AddSingleton(new Mock<IComponentStateService>(MockBehavior.Loose).Object);
    }

    [Fact]
    public async Task SetSelectItems_WhenSelectionIsOff_Throws()
    {
        var cut = Render<PhDataGrid<TestItem>>(ps => ps
            .Add(p => p.StateID, "grid-set-select-off")
            .Add(p => p.DefaultSortColumn, nameof(TestItem.Id))
            .Add(p => p.SelectionType, SelectionTypeEnum.Off));

        var act = async () => await cut.InvokeAsync(() => cut.Instance.SetSelectItems([new TestItem(1)]));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Selection is disabled for this grid.");
    }

    [Fact]
    public async Task SetSelectItems_WhenSingleSelectAndMultipleItems_Throws()
    {
        var cut = Render<PhDataGrid<TestItem>>(ps => ps
            .Add(p => p.StateID, "grid-set-select-single-select")
            .Add(p => p.DefaultSortColumn, nameof(TestItem.Id))
            .Add(p => p.SelectionType, SelectionTypeEnum.SingleSelect));

        var act = async () => await cut.InvokeAsync(() => cut.Instance.SetSelectItems([new TestItem(1), new TestItem(2)]));

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot select multiple items when SelectionType is SingleSelect.");
    }

    [Fact]
    public async Task SetSelectItems_WhenMultiSelect_RaisesOnSelectedItemsChangedWithItems()
    {
        var tcs = new TaskCompletionSource<IReadOnlyCollection<TestItem>>(TaskCreationOptions.RunContinuationsAsynchronously);

        var onSelectedItemsChanged = EventCallback.Factory.Create<HashSet<TestItem>>(
            receiver: new object(),
            callback: (HashSet<TestItem> items) =>
            {
                tcs.TrySetResult(items.ToArray());
                return Task.CompletedTask;
            });

        var cut = Render<PhDataGrid<TestItem>>(ps => ps
            .Add(p => p.StateID, "grid-set-select-multiselect")
            .Add(p => p.DefaultSortColumn, nameof(TestItem.Id))
            .Add(p => p.SelectionType, SelectionTypeEnum.MultiSelect)
            .Add(p => p.OnSelectedItemsChanged, onSelectedItemsChanged));

        var expected = new[] { new TestItem(1), new TestItem(2) };

        await cut.InvokeAsync(() => cut.Instance.SetSelectItems([.. expected]));

        var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        received.Should().BeEquivalentTo(expected, o => o.WithoutStrictOrdering());
    }

    [Fact]
    public async Task SetSelectItems_WhenSingleSelect_RaisesOnSelectedItemChangedWithItem()
    {
        var tcs = new TaskCompletionSource<TestItem?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var onSelectedItemChanged = EventCallback.Factory.Create<TestItem?>(
            receiver: new object(),
            callback: (TestItem? item) =>
            {
                tcs.TrySetResult(item);
                return Task.CompletedTask;
            });

        var cut = Render<PhDataGrid<TestItem>>(ps => ps
            .Add(p => p.StateID, "grid-set-select-single-select-callback")
            .Add(p => p.DefaultSortColumn, nameof(TestItem.Id))
            .Add(p => p.SelectionType, SelectionTypeEnum.SingleSelect)
            .Add(p => p.OnSelectedItemChanged, onSelectedItemChanged));

        var expected = new TestItem(42);

        await cut.InvokeAsync(() => cut.Instance.SetSelectItems([expected]));

        var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2), Xunit.TestContext.Current.CancellationToken);
        received.Should().Be(expected);
    }
}