using Bunit;
using Pharos.Common.Web.Components.Drawer;
using Pharos.Common.Web.Components.Legend;

namespace Pharos.Common.Web.Tests;

public class PhLegendTests : BunitContext
{
    [Fact]
    public void RendersStatusSection_WhenLegendStatusesArePresent()
    {
        // Arrange
        var legendStatuses = new List<TubeStatus> { TubeStatus.NotIdentified, TubeStatus.InProgress };
        var statusHeaderText = "Legend:";
        var getStatusText = new Func<TubeStatus, string>(status => status.ToString());

        // Act
        var cut = Render<PhLegend<TubeStatus>>(parameters => parameters
            .Add(p => p.LegendStatuses, legendStatuses)
            .Add(p => p.StatusHeaderText, statusHeaderText)
            .Add(p => p.GetStatusText, getStatusText)
            .Add(p => p.IsAnyHighPriority, true)
            .Add(p => p.IsAnyManualEntry, true)
        );

        // Assert
        cut.Find(".ph-legend__section-heading").MarkupMatches($"<div class=\"ph-legend__section-heading\">{statusHeaderText}</div>");
        var statusItems = cut.FindAll(".ph-legend__item");
        Assert.Equal(4, statusItems.Count);
    }

    [Fact]
    public void RendersErrorSection_WhenLegendErrorsArePresent()
    {
        // Arrange
        var legendErrors = new List<LegendError>
            {
                new() { Error = TubeError.UnreadableBarcode, Text = "Unreadable barcode", IsClickable = true, Count = 1, GroupOrder = 2, Order=1 },
                new() { Error = TubeError.TubeGripError, Text = "Transfer not possible", IsClickable = false, Count = 2, GroupOrder = 2, Order=2 }
            };
        var errorHeaderText = "Errors:";

        // Act
        var cut = Render<PhLegend<TubeStatus>>(parameters => parameters
            .Add(p => p.LegendErrors, legendErrors)
            .Add(p => p.ErrorHeaderText, errorHeaderText)
        );

        // Assert
        cut.Find(".ph-legend__section-heading").MarkupMatches($"<div class=\"ph-legend__section-heading\">{errorHeaderText}</div>");
        var errorItems = cut.FindAll(".ph-legend__item");
        Assert.Equal(2, errorItems.Count);
    }

}