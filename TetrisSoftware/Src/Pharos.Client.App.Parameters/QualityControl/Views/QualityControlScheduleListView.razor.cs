using System.Globalization;
using Pharos.Client.App.Parameters.QualityControl.ViewModels;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.QualityControl.Views;

public partial class QualityControlScheduleListView
{
    private Dictionary<string, string> _methodNameDict = [];
    [Inject] public QualityControlScheduleListViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _methodNameDict = await ViewModel.GetMethodNames();
    }

    private async Task EditPeriodically(QualityControlSchedule item)
    {
        var viewModel = new EditQualityControlScheduleViewModel(item.Id, item.Periodically, item.StartAfter, item.RunEvery);

        var parameters = new ModalParameters
        {
            { nameof(EditQualityControlScheduleModalView.ViewModel), viewModel }
        };
        var model = ModalService.Show<EditQualityControlScheduleModalView>(new ModalOptions
        {
            Title = Loc["EditQualityControlScheduleModalView_Title"],
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small
        }, parameters);
        var result = await model.Result;

        if (!result.Canceled)
        {
            await ViewModel.UpdatePeriodicalInformation(item, viewModel.Periodically, viewModel.StartAfter, viewModel.RunEvery);
        }
    }

    private string GetPeriodicallyDisplayValue(QualityControlSchedule item)
    {
        return item.Periodically switch
        {
            Periodically.No => Loc["Periodically_No_Link"],
            Periodically.YesByNumberOfTests => string.Format(CultureInfo.InvariantCulture, Loc["Periodically_YesByNumberOfTests_Link"], item.RunEvery, item.StartAfter),
            Periodically.YesByTime => string.Format(CultureInfo.InvariantCulture, Loc["Periodically_YesByTime_Link"], item.RunEvery, item.StartAfter),
            _ => string.Empty
        };
    }
}