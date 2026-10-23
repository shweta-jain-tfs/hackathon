namespace Pharos.Common.Web.Tests.DataGrid;

public class FilterTitleTests
{
    [Fact]
    public void EffectiveFilterTitle_FallsBackToTitle_WhenFilterTitleIsNotSet()
    {
        var column = new TestColumn<Person, string>("FirstName", title: "First Name");

        column.EffectiveFilterTitle.Should().Be("First Name");
    }

    [Fact]
    public void EffectiveFilterTitle_UsesFilterTitle_WhenProvided()
    {
        var column = new TestColumn<Person, string>("FirstName", title: "First Name", filterTitle: "Search by first name");

        column.EffectiveFilterTitle.Should().Be("Search by first name");
    }
}
