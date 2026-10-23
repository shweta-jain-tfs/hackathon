using Microsoft.AspNetCore.Components;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Reagent;
using Pharos.Common.Web.Utilities;
using System.Globalization;

namespace Pharos.Client.App.Reagents.Views.BottleDrawer;

public partial class BottleReagentView : ComponentBase
{
    [Parameter] public BottlePosition BottlePosition { get; set; } = default!;
    [Parameter] public EventCallback<int> OnBottleSelected { get; set; }
    [Parameter] public int? SelectedBottleId { get; set; }
    [Parameter] public int? FlashingBottleId { get; set; }
    [Parameter] public BottleStatus? SelectedLegendStatus { get; set; }

    private string BottleReagentCss => new CssBuilder("bottle-reagent")
        .AddClass("bottle-reagent__circle", BottlePosition.Size == BottlePositionSize.Small)
        .Build();

    private string BottlePositionCss => new CssBuilder("bottle-reagent__positions")
        .AddClass("bottle-reagent__positions--row", BottlePosition.Size == BottlePositionSize.Large)
        .AddClass("bottle-reagent__positions--col", BottlePosition.Size == BottlePositionSize.Small)
        .Build();

    private string GetArticleTypeText()
    {
        var articleTypeText = BottlePosition.ArticleType.ToLocalizedString();
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(articleTypeText.ToLower());
    }

    private void BottleSelected(int selectedBottleId)
    {
        OnBottleSelected.InvokeAsync(selectedBottleId);
    }

    private bool IsSelected(BottleDetail bottleDetails)
    {
        return SelectedBottleId != 0 && SelectedBottleId == bottleDetails.Id;
    }

    private bool IsFlashing(BottleDetail bottleDetails)
    {
        return FlashingBottleId != null && FlashingBottleId == bottleDetails.Id;
    }

    private bool IsDimmed(BottleDetail bottleDetails)
    {
        return SelectedLegendStatus.HasValue
            && bottleDetails.Status.ToBottleStatus() != SelectedLegendStatus;
    }

    private bool IsHighlighted(BottleDetail bottleDetails)
    {
        return SelectedLegendStatus.HasValue
            && bottleDetails.Status.ToBottleStatus() == SelectedLegendStatus;
    }
}
