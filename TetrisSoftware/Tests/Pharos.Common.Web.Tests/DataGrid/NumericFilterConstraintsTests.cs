using Bunit;
using Pharos.Common.Web.Components.DataGrid.Columns;
using Pharos.Common.Web.Components.DataGrid.Filters.Constraints;
using Pharos.Common.Web.Components.DataGrid.Filters.Models;
using Pharos.Common.Web.Components.DataGrid.Filters.Quick;
using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Common.Web.Tests.DataGrid;

public class NumericFilterConstraintsTests : BunitContext
{
    public NumericFilterConstraintsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void QuickNumericFilter_AllowsNegativeNumbersWhenConfigured()
    {
        var filter = CreateFilter(new NumericFilterConstraints<int>(
            allowNegativeNumbers: true));

        var cut = Render<PhQuickNumericColumnFilter<Person, int>>(parameters => parameters
            .Add(p => p.ColumnFilter, filter));

        cut.FindComponents<PhNumberField<double?>>().Should().AllSatisfy(
            numberField => numberField.Instance.AllowNegativeNumbers.Should().BeTrue());
    }

    [Fact]
    public void NumericFilterConstraints_SupportsNullableColumns()
    {
        IFilterConstraints<int?> constraints = new NumericFilterConstraints<int>();

        constraints.Should().BeOfType<NumericFilterConstraints<int>>();
    }

    private static NumericColumnFilter<Person, int> CreateFilter(IFilterConstraints<int> constraints)
    {
        var column = new Column<Person, int>();
        typeof(Column<Person, int>)
            .GetProperty(nameof(Column<Person, int>.FilterConstraints))!
            .SetValue(column, constraints);

        return new NumericColumnFilter<Person, int>(column);
    }
}
