using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.DataGrid.Filters.Models;
using Pharos.Common.Web.Components.DataGrid.Filters.Quick;
using Pharos.Common.Web.Components.Loader;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.QuickFilter;
using Pharos.Common.Web.Models.DataGrid;
using Pharos.Common.Web.Services;

namespace Pharos.Common.Web.Tests.DataGrid;

public class CustomQuickFilterTests : BunitContext
{
    public CustomQuickFilterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(_ => new Mock<LoaderService>().Object);
        Services.AddSingleton(_ => new Mock<IModalService>().Object);
        Services.AddSingleton(_ => new Mock<IComponentStateService>().Object);
    }

    private static IQueryable<Person> People =>
        new[]
        {
            new Person(1, "Alice", "Anderson"),
            new Person(2, "Bob", "Brown")
        }.AsQueryable();

    [Fact]
    public void PhDataGridFilter_RendersCustomQuickFilterAlongsideColumnQuickFilter()
    {
        var column = CreateQuickFilterColumn("FirstName", "First name");
        var customFilter = CreateCustomQuickFilter("CustomName", "Custom name");

        var cut = Render<PhDataGridFilter<Person>>(parameters => parameters
            .Add(p => p.Filters, new List<IQuickFilterSource> { column, customFilter }));

        cut.FindAll("input").Should().HaveCount(2);
        cut.Markup.Should().Contain("First name");
        cut.Markup.Should().Contain("Custom name");
    }

    [Fact]
    public async Task PhDataGridFilter_ClearAll_ClearsColumnAndCustomFilters()
    {
        var column = CreateQuickFilterColumn("FirstName", "First name", initialValue: "Alice");
        var customFilter = CreateCustomQuickFilter("CustomName", "Custom name", initialValue: "Bob");

        var cut = Render<PhDataGridFilter<Person>>(parameters => parameters
            .Add(p => p.Filters, new List<IQuickFilterSource> { column, customFilter }));

        await cut.InvokeAsync(() => cut.FindComponent<PhFilterActions>().Instance.OnClearAll.InvokeAsync());

        ((StringColumnFilter<Person, string>)column.Filter!).Value.Should().BeEmpty();
        ((StringColumnFilter<Person, string>)customFilter.Filter).Value.Should().BeEmpty();
    }

    [Fact]
    public async Task PhDataGridFilter_ResetToDefault_ResetsColumnAndCustomFilters()
    {
        var column = CreateQuickFilterColumn("FirstName", "First name", quickFilterDefault: "Ann", initialValue: "Alice");
        var customFilter = CreateCustomQuickFilter("CustomName", "Custom name", quickFilterDefault: "Bob", initialValue: "Charlie");

        var cut = Render<PhDataGridFilter<Person>>(parameters => parameters
            .Add(p => p.Filters, new List<IQuickFilterSource> { column, customFilter }));

        await cut.InvokeAsync(() => cut.FindComponent<PhFilterActions>().Instance.OnResetToDefault.InvokeAsync());

        ((StringColumnFilter<Person, string>)column.Filter!).Value.Should().Be("Ann");
        ((StringColumnFilter<Person, string>)customFilter.Filter).Value.Should().Be("Bob");
    }

    [Fact]
    public void PhDataGrid_RendersCustomQuickFilter()
    {
        var customFilter = CreateCustomQuickFilter("CustomName", "Custom name");

        var cut = Render<TestCustomQuickFilterDataGrid>(parameters => parameters
            .Add(p => p.Items, People)
            .Add(p => p.CustomQuickFilters, new[] { customFilter }));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Custom name"));
    }

    [Fact]
    public void PhDataGrid_Throws_WhenCustomFilterPropertyNameDuplicatesColumnFilter()
    {
        var customFilter = CreateCustomQuickFilter("FirstName", "Duplicate first name");

        var act = () => Render<TestCustomQuickFilterDataGrid>(parameters => parameters
            .Add(p => p.Items, People)
            .Add(p => p.CustomQuickFilters, new[] { customFilter }));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Filter PropertyName values must be unique*");
    }

    private static TestColumn<Person, string> CreateQuickFilterColumn(
        string propertyName,
        string title,
        string? quickFilterDefault = null,
        string? initialValue = null)
    {
        var column = new TestColumn<Person, string>(propertyName, title, quickFilter: true, quickFilterDefault: quickFilterDefault)
        {
            QuickFilterComponentType = typeof(PhQuickStringColumnFilter<Person, string>)
        };

        column.Filter = new StringColumnFilter<Person, string>(column)
        {
            Value = initialValue
        };

        return column;
    }

    private static CustomQuickFilter CreateCustomQuickFilter(
        string propertyName,
        string title,
        string? quickFilterDefault = null,
        string? initialValue = null)
    {
        var column = new TestColumn<Person, string>(propertyName, title, quickFilter: true, quickFilterDefault: quickFilterDefault);
        var filter = new StringColumnFilter<Person, string>(column)
        {
            Value = initialValue
        };

        return new CustomQuickFilter(filter, typeof(PhQuickStringColumnFilter<Person, string>));
    }
}
