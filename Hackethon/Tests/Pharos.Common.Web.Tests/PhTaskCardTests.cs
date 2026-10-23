using Bunit;
using Pharos.Common.Web.Components.TaskCard;

namespace Pharos.Common.Web.Tests;

public class PhTaskCardTests
{
    [Fact]
    public void Time_ShouldShow_When_DisplayTimeIsSetToTrue()
    {
        // Arrange
        using var ctx = new BunitContext();
        var taskCard = ctx.Render<PhTaskCard>(parameters => parameters
                                                                     .Add(p => p.DisplayTime, true)
                                                                     .Add(p => p.Time, "1d"));

        // Act
        var time = taskCard.Find(".ph-task-card__time");

        // Assert
        time.Should().NotBeNull();
        time.InnerHtml.Should().Be("1d");
    }

    [Fact]
    public void Time_ShouldNotShow_When_DisplayTimeIsSetToFalse()
    {
        // Arrange
        using var ctx = new BunitContext();
        var taskCard = ctx.Render<PhTaskCard>(parameters => parameters
                                                                     .Add(p => p.DisplayTime, false)
                                                                     .Add(p => p.Time, "1d"));

        // Act
        var time = taskCard.Find(".ph-task-card__time");

        // Assert
        time.Should().NotBeNull();
        time.InnerHtml.Should().Be(string.Empty);
    }
}