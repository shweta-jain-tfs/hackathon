using Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;
using Pharos.Client.Common.Models;
using Xunit;

namespace Pharos.Client.App.InstrumentManagement.Tests;

public class ProcessListViewTests
{
    [Fact]
    public void ShouldDisplayProcessTime_ReturnsFalse_ForMinValue()
    {
        var item = new ProcessListItem { Time = DateTimeOffset.MinValue };
        var result = ProcessListLogic.ShouldDisplayProcessTime(item, InstrumentState.Assay);
        Assert.False(result);
    }

    [Fact]
    public void ShouldDisplayProcessTime_ReturnsTrue_ForValidDate_And_AssayState()
    {
        var item = new ProcessListItem { Time = DateTimeOffset.UtcNow };
        var result = ProcessListLogic.ShouldDisplayProcessTime(item, InstrumentState.Assay);
        Assert.True(result);
    }

    [Fact]
    public void ShouldDisplayProcessTime_ReturnsFalse_ForNonAssayState()
    {
        var item = new ProcessListItem { Time = DateTimeOffset.UtcNow };
        var result = ProcessListLogic.ShouldDisplayProcessTime(item, InstrumentState.Standby);
        Assert.False(result);
    }

    [Fact]
    public void ShouldDisplayProcessTime_ReturnsFalse_ForUnixEpoch1970()
    {
        // Arrange: 1970-01-01T00:00:00Z is the Unix epoch
        var item = new ProcessListItem { Time = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var result = ProcessListLogic.ShouldDisplayProcessTime(item, InstrumentState.Assay);

        // Assert: Should be false, as 1970 is considered invalid in this context
        Assert.False(result);
    }
}           