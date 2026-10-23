using System.Globalization;
using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.DataGrid.Columns;
using Pharos.Common.Web.Components.Loader;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Models.DataGrid;
using Pharos.Common.Web.Services;

namespace Pharos.Common.Web.Tests.DataGrid;

/// <summary>
/// Component-level tests for DataGrid: Filter Handling.
/// <para>
/// The unit tests in <see cref="ColumnFilterCharacterizationTests"/> exercise the filter models
/// directly. These tests cover the part that only exists once a real grid renders: that a
/// multi-select column whose data source arrives <b>after</b> the first render still ends up with a
/// working quick filter. That timing is the reason the filter pulls its values from a provider
/// instead of having them pushed in from <c>OnParametersSet</c>.
/// </para>
/// </summary>
public class MultiSelectColumnFilterComponentTests : BunitContext
{
    public MultiSelectColumnFilterComponentTests()
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

    private static IReadOnlyList<Person> FilterablePeople =>
        new[]
        {
            new Person(1, "Alice", "Anderson"),
            new Person(2, "Alice", "Brown"),
            new Person(3, "Bob", "Anderson")
        };

    private static IReadOnlyList<PersonWithStatus> PeopleWithStatuses =>
        new[]
        {
            new PersonWithStatus(1, "Alice", WorkflowStatus.Active),
            new PersonWithStatus(2, "Bob", WorkflowStatus.Archived),
            new PersonWithStatus(3, "Charlie", WorkflowStatus.Draft)
        };

    [Fact]
    public void QuickFilter_RendersOptions_WhenEmptyDataSourceIsPopulatedInPlace()
    {
        // The common consumer pattern: the field is initialised to an empty dictionary and filled in
        // by an async load that finishes after the first render.
        var dataSource = new Dictionary<string, string>();

        var cut = Render<TestMultiSelectDataGrid>(parameters => parameters
            .Add(p => p.Items, People)
            .Add(p => p.FilterDataSource, dataSource));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Alice"));
        cut.Markup.Should().NotContain("Alice option");

        dataSource["alice"] = "Alice option";
        dataSource["bob"] = "Bob option";

        cut.Render(parameters => parameters
            .Add(p => p.Items, People)
            .Add(p => p.FilterDataSource, dataSource));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Alice option");
            cut.Markup.Should().Contain("Bob option");
        });
    }

    [Fact]
    public void MultiSelectFilter_Throws_WhenDataSourceIsNullOnFirstRender()
    {
        // The filter type is chosen once, so a null data source would silently fall back to the
        // text filter forever. Column now fails fast instead.
        var act = () => Render<TestMultiSelectDataGrid>(parameters => parameters
            .Add(p => p.Items, People));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MultiSelectFilterItems must be initialized*");
    }

    [Fact]
    public void QuickFilter_RendersAddedOptions_WhenDataSourceIsReplaced()
    {
        var cut = Render<TestMultiSelectDataGrid>(parameters => parameters
            .Add(p => p.Items, People)
            .Add(p => p.FilterDataSource, new Dictionary<string, string> { ["alice"] = "Alice option" }));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Alice option"));

        // A refreshed lookup must reach the rendered quick filter without the column having to push
        // the new values into the filter.
        cut.Render(parameters => parameters
            .Add(p => p.Items, People)
            .Add(p => p.FilterDataSource, new Dictionary<string, string>
            {
                ["alice"] = "Alice option",
                ["bob"] = "Bob option"
            }));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Bob option"));
    }

    [Fact]
    public void QuickFilter_AppliesTwoSimultaneousColumnFilters()
    {
        GridItemsProviderRequest<Person>? lastRequest = null;

        var cut = RenderMultiColumnFilterGrid(
            request =>
            {
                lastRequest = request;
                var filteredPeople = ApplyFilters(FilterablePeople, request.ColumnFilters);

                return ValueTask.FromResult(GridItemsProviderResult.From<Person>(filteredPeople.ToList(), filteredPeople.Count));
            },
            new Dictionary<string, string>
            {
                ["Alice"] = "Alice option",
                ["Bob"] = "Bob option"
            },
            new Dictionary<string, string>
            {
                ["Anderson"] = "Anderson option",
                ["Brown"] = "Brown option"
            });

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Alice Anderson");
            cut.Markup.Should().Contain("Alice Brown");
            cut.Markup.Should().Contain("Bob Anderson");
        });

        ToggleCheckbox(cut, "Alice option");

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Alice Anderson");
            cut.Markup.Should().Contain("Alice Brown");
            cut.Markup.Should().NotContain("Bob Anderson");
        });

        ToggleCheckbox(cut, "Anderson option");

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Alice Anderson");
            cut.Markup.Should().NotContain("Alice Brown");
            cut.Markup.Should().NotContain("Bob Anderson");

            lastRequest.Should().NotBeNull();
            lastRequest!.ColumnFilters!
                .Where(filter => filter.FilterExpressions.Any())
                .Should().HaveCount(2);
        });
    }

    [Fact]
    public void QuickFilter_UsesMultiSelectDataSource_ForEnumColumn()
    {
        GridItemsProviderRequest<PersonWithStatus>? lastRequest = null;

        var cut = RenderEnumFilterGrid(
            request =>
            {
                lastRequest = request;
                var filteredPeople = ApplyFilters(PeopleWithStatuses, request.ColumnFilters);

                return ValueTask.FromResult(GridItemsProviderResult.From<PersonWithStatus>(filteredPeople.ToList(), filteredPeople.Count));
            },
            new Dictionary<string, string>
            {
                ["1"] = "Active option",
                ["2"] = "Archived option"
            });

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Active option");
            cut.Markup.Should().Contain("Archived option");
        });

        ToggleCheckbox(cut, "Active option");

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Alice");
            cut.Markup.Should().NotContain("Bob");
            cut.Markup.Should().NotContain("Charlie");

            lastRequest.Should().NotBeNull();
            lastRequest!.ColumnFilters!
                .Single(filter => filter.PropertyName == nameof(PersonWithStatus.Status))
                .FilterExpressions
                .Should().ContainSingle(expression => Equals(
                    expression,
                    new PropertyFilterExpression<string>(nameof(PersonWithStatus.Status), PropertyFilterOperator.EqualsTo, "1")));
        });
    }

    private IRenderedComponent<PhDataGrid<Person>> RenderMultiColumnFilterGrid(
        GridItemsProvider<Person> itemsProvider,
        Dictionary<string, string> firstNameFilterDataSource,
        Dictionary<string, string> lastNameFilterDataSource)
    {
        return Render<PhDataGrid<Person>>(parameters => parameters
            .Add(p => p.StateID, "test-multi-column-filter-grid")
            .Add(p => p.ItemsProvider, itemsProvider)
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<Column<Person, string>>(0);
                builder.AddAttribute(1, nameof(Column<Person, string>.Title), "First name");
                builder.AddAttribute(2, nameof(Column<Person, string>.Property), (Expression<Func<Person, string>>)(person => person.FirstName));
                builder.AddAttribute(3, nameof(Column<Person, string>.QuickFilter), true);
                builder.AddAttribute(5, nameof(Column<Person, string>.MultiSelectFilterItems), firstNameFilterDataSource);
                builder.CloseComponent();

                builder.OpenComponent<Column<Person, string>>(6);
                builder.AddAttribute(7, nameof(Column<Person, string>.Title), "Last name");
                builder.AddAttribute(8, nameof(Column<Person, string>.Property), (Expression<Func<Person, string>>)(person => person.LastName));
                builder.AddAttribute(9, nameof(Column<Person, string>.QuickFilter), true);
                builder.AddAttribute(11, nameof(Column<Person, string>.MultiSelectFilterItems), lastNameFilterDataSource);
                builder.CloseComponent();

                builder.OpenComponent<Column<Person, string>>(12);
                builder.AddAttribute(13, nameof(Column<Person, string>.Title), "Full name");
                builder.AddAttribute(14, nameof(Column<Person, string>.Property), (Expression<Func<Person, string>>)(person => person.FullName));
                builder.CloseComponent();
            }));
    }

    private IRenderedComponent<PhDataGrid<PersonWithStatus>> RenderEnumFilterGrid(
        GridItemsProvider<PersonWithStatus> itemsProvider,
        Dictionary<string, string> statusFilterDataSource)
    {
        return Render<PhDataGrid<PersonWithStatus>>(parameters => parameters
            .Add(p => p.StateID, "test-enum-multiselect-grid")
            .Add(p => p.ItemsProvider, itemsProvider)
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent<Column<PersonWithStatus, string>>(0);
                builder.AddAttribute(1, nameof(Column<PersonWithStatus, string>.Title), "Name");
                builder.AddAttribute(2, nameof(Column<PersonWithStatus, string>.Property), (Expression<Func<PersonWithStatus, string>>)(person => person.Name));
                builder.CloseComponent();

                builder.OpenComponent<Column<PersonWithStatus, WorkflowStatus>>(3);
                builder.AddAttribute(4, nameof(Column<PersonWithStatus, WorkflowStatus>.Title), "Status");
                builder.AddAttribute(5, nameof(Column<PersonWithStatus, WorkflowStatus>.Property), (Expression<Func<PersonWithStatus, WorkflowStatus>>)(person => person.Status));
                builder.AddAttribute(6, nameof(Column<PersonWithStatus, WorkflowStatus>.QuickFilter), true);
                builder.AddAttribute(8, nameof(Column<PersonWithStatus, WorkflowStatus>.MultiSelectFilterItems), statusFilterDataSource);
                builder.CloseComponent();
            }));
    }

    private static void ToggleCheckbox<TComponent>(IRenderedComponent<TComponent> cut, string labelText)
        where TComponent : IComponent
    {
        var label = cut.FindAll("label.ph-checkbox__label")
            .Single(x => x.TextContent.Contains(labelText, StringComparison.Ordinal));

        label.QuerySelector("input")!.Change(true);
    }

    private static List<Person> ApplyFilters(
        IReadOnlyList<Person> people,
        IEnumerable<IColumnFilter>? filters)
    {
        return ApplyFilters(people, filters, Matches).ToList();
    }

    private static List<PersonWithStatus> ApplyFilters(
        IReadOnlyList<PersonWithStatus> people,
        IEnumerable<IColumnFilter>? filters)
    {
        return ApplyFilters(people, filters, Matches).ToList();
    }

    private static IEnumerable<TItem> ApplyFilters<TItem>(
        IEnumerable<TItem> items,
        IEnumerable<IColumnFilter>? filters,
        Func<TItem, PropertyFilterExpression, bool> matches)
    {
        var filteredItems = items;

        foreach (var filter in filters ?? [])
        {
            var expressions = filter.FilterExpressions.ToList();
            if (expressions.Count == 0)
            {
                continue;
            }

            filteredItems = filter.LogicalOperator == LogicalFilterOperator.Or
                ? filteredItems.Where(item => expressions.Any(expression => matches(item, expression)))
                : filteredItems.Where(item => expressions.All(expression => matches(item, expression)));
        }

        return filteredItems;
    }

    private static string ValueOf(PropertyFilterExpression expression) =>
        Convert.ToString(expression.UntypedValue, CultureInfo.InvariantCulture) ?? string.Empty;

    private static bool Matches(Person person, PropertyFilterExpression expression)
    {
        return expression switch
        {
            { PropertyName: nameof(Person.FirstName), Operator: PropertyFilterOperator.EqualsTo } =>
                person.FirstName == ValueOf(expression),
            { PropertyName: nameof(Person.LastName), Operator: PropertyFilterOperator.EqualsTo } =>
                person.LastName == ValueOf(expression),
            { PropertyName: nameof(Person.FirstName), Operator: PropertyFilterOperator.Contains } =>
                person.FirstName.Contains(ValueOf(expression), StringComparison.Ordinal),
            { PropertyName: nameof(Person.LastName), Operator: PropertyFilterOperator.Contains } =>
                person.LastName.Contains(ValueOf(expression), StringComparison.Ordinal),
            _ => false
        };
    }

    private static bool Matches(PersonWithStatus person, PropertyFilterExpression expression)
    {
        return expression switch
        {
            { PropertyName: nameof(PersonWithStatus.Name), Operator: PropertyFilterOperator.Contains } =>
                person.Name.Contains(ValueOf(expression), StringComparison.Ordinal),
            { PropertyName: nameof(PersonWithStatus.Status), Operator: PropertyFilterOperator.EqualsTo } =>
                Convert.ToInt32(person.Status, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) ==
                ValueOf(expression),
            _ => false
        };
    }
}
