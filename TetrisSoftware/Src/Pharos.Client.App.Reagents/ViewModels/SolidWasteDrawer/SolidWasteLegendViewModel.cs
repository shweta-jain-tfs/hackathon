using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Legend;
using Pharos.Common.Web.Components.SolidWaste;

namespace Pharos.Client.App.Reagents.ViewModels.SolidWasteDrawer;

public class SolidWasteLegendViewModel : ViewModelBase
{
    public List<LegendError> LegendErrors
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(LegendErrors));
        }
    } = new();

    public void UpdateTaskCardLegend(IEnumerable<TaskCard> taskCards)
    {
        LegendErrors = taskCards
            .Where(taskCard => taskCard.CardType == TaskCardType.SolidWasteDrawerOpen)
            .Select(_ => new LegendError
            {
                Error = SolidWasteError.CloseDrawer,
                Text = SolidWasteError.CloseDrawer.ToLocalizedString("SolidWasteLegend"),
                IsClickable = false,
                Count = 1,
                GroupOrder = 1,
                Order = (int)SolidWasteError.CloseDrawer
            })
            .ToList();
    }
}
