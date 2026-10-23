using Pharos.Server.Application.Settings.General;

namespace Pharos.Server.Settings.DropdownProviders;

public class TimeZoneDropdownProviderTests
{
    private readonly TimeZoneDropdownProvider _provider = new();


    [Fact]
    public async Task GetOptionsAsync_ContainsUTC()
    {
        // Act
        var result = await _provider.GetOptionsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(result, option => option.Value.Equals("UTC", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetOptionsAsync_AllOptionsHaveValueAndDisplayName()
    {
        // Act
        var result = await _provider.GetOptionsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.All(result, option =>
        {
            Assert.False(string.IsNullOrWhiteSpace(option.Value), "Option.Value should not be null or empty");
            Assert.False(string.IsNullOrWhiteSpace(option.DisplayName), "Option.DisplayName should not be null or empty");
        });
    }

    [Fact]
    public async Task GetOptionsAsync_DisplayNamesAreNotEmpty()
    {
        // Act
        var result = await _provider.GetOptionsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.All(result, option =>
        {
            Assert.False(string.IsNullOrWhiteSpace(option.DisplayName), "DisplayName should contain time zone information");
            Assert.True(option.DisplayName.Length > 3, "DisplayName should contain meaningful time zone information");
        });
    }

    [Fact]
    public async Task GetOptionsAsync_NoDuplicateValues()
    {
        // Act
        var result = await _provider.GetOptionsAsync(TestContext.Current.CancellationToken);

        // Assert
        var distinctCount = result.Select(o => o.Value).Distinct().Count();
        Assert.Equal(result.Count, distinctCount);
    }

    [Fact]
    public async Task GetOptionsAsync_SortedByUtcOffset()
    {
        // Act
        var result = await _provider.GetOptionsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEmpty(result);

        // Verify sorting by checking that UTC offsets are in ascending order
        var utcIndex = result.FindIndex(o => o.Value.Equals("UTC", StringComparison.OrdinalIgnoreCase));
        Assert.True(utcIndex >= 0, "UTC should be present in the list");

        // Find a timezone with negative offset (e.g., Pacific) and positive offset (e.g., Tokyo)
        var pacificIndex = result.FindIndex(o => o.Value.Contains("Pacific") || o.Value.Contains("America"));
        var tokyoIndex = result.FindIndex(o => o.Value.Contains("Tokyo") || o.Value.Contains("Asia"));

        // If both exist, verify Pacific (negative offset) comes before UTC, and Tokyo (positive offset) comes after
        if (pacificIndex >= 0)
        {
            Assert.True(pacificIndex < utcIndex, "Negative UTC offset timezones should come before UTC");
        }
        if (tokyoIndex >= 0)
        {
            Assert.True(tokyoIndex > utcIndex, "Positive UTC offset timezones should come after UTC");
        }
    }
}
