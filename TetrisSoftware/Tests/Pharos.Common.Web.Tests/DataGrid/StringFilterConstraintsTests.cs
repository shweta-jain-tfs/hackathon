using Bunit;
using Pharos.Common.Web.Components.DataGrid.Columns;
using Pharos.Common.Web.Components.DataGrid.Filters.Constraints;
using Pharos.Common.Web.Components.DataGrid.Filters.Models;
using Pharos.Common.Web.Components.DataGrid.Filters.Quick;

namespace Pharos.Common.Web.Tests.DataGrid;

public class StringFilterConstraintsTests : BunitContext
{
    public StringFilterConstraintsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void QuickStringFilter_AppliesConfiguredMaxLength()
    {
        var filter = CreateFilter(12);

        var cut = Render<PhQuickStringColumnFilter<Person, string>>(parameters => parameters
            .Add(p => p.ColumnFilter, filter));

        cut.Find("input").GetAttribute("maxlength").Should().Be("12");
    }

    [Fact]
    public void QuickStringFilter_PreservesDefaultMaxLengthWithoutSettings()
    {
        var column = new Column<Person, string>();
        var filter = new StringColumnFilter<Person, string>(column);

        var cut = Render<PhQuickStringColumnFilter<Person, string>>(parameters => parameters
            .Add(p => p.ColumnFilter, filter));

        cut.Find("input").GetAttribute("maxlength").Should().Be("50");
    }

    [Fact]
    public void StringFilterConstraints_RejectsNonPositiveMaxLength()
    {
        var action = () => new StringFilterConstraints(0);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static StringColumnFilter<Person, string> CreateFilter(int maxLength)
    {
        var column = new Column<Person, string>();
        typeof(Column<Person, string>)
            .GetProperty(nameof(Column<Person, string>.FilterConstraints))!
            .SetValue(column, new StringFilterConstraints(maxLength));

        return new StringColumnFilter<Person, string>(column);
    }
}
