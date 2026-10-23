namespace Pharos.Server.Parameters.Application.Services;

public class AlphanumericStringComparerTest
{
    const string _str1 = "abc123";

    [Fact]
    public void Compare_EqualStrings_ReturnsZero()
    {
        // Arrange
        var comparer = new AlphanumericStringComparer();
        var str2 = "abc123";
        // Act
        var result = comparer.Compare(_str1, str2);
        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Compare_DifferentStrings_ReturnsNonZero()
    {
        // Arrange
        var comparer = new AlphanumericStringComparer();
        var str2 = "abc124";
        // Act
        var result = comparer.Compare(_str1, str2);
        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Compare_NullAndNonNullStrings_ReturnsNegative()
    {
        // Arrange
        var comparer = new AlphanumericStringComparer();
        string? str1 = null;
        var str2 = "abc123";
        // Act
        var result = comparer.Compare(str1, str2);
        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void Compare_NonNullAndNullStrings_ReturnsPositive()
    {
        // Arrange
        var comparer = new AlphanumericStringComparer();
        string? str2 = null;
        // Act
        var result = comparer.Compare(_str1, str2);
        // Assert
        Assert.True(result > 0);
    }

    [Theory]
    [InlineData("c1", "c10", "c2", "c1", "c2", "c10", false)]
    [InlineData("c10", "c2", "c1", "c1", "c2", "c10", false)]
    [InlineData("c1", "c10", "c2", "c10", "c2", "c1", true)]
    [InlineData("c1.0", "c10", "c1.01", "c1.0", "c1.01", "c10", false)]
    [InlineData("c1", "c10", "c1.01", "c1", "c1.01", "c10", false)]
    public void SortAlphaNumericalList(string item0, string item1, string item2, string expected0, string expected1, string expected2, bool descending)
    {
        var comparer = new AlphanumericStringComparer(descending: descending);
        var list = new List<string> { item0, item1, item2 };
        list.Sort(comparer);

        Assert.Equal(expected0, list[0]);
        Assert.Equal(expected1, list[1]);
        Assert.Equal(expected2, list[2]);
    }
}
