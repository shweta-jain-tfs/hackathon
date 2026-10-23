namespace Pharos.Client.App.Parameters.Panels.ViewModels;

public class PanelsDetailsPageViewModel(
    IDetailsPageDataService<MethodPanelDetails> dataService) : DetailsPageViewModel<PanelsDetailsViewModel, MethodPanelDetails>(dataService, null)
{
    protected override Task<MethodPanelDetails> CreateDefaultModel()
    {
        return Task.FromResult(new MethodPanelDetails
        {
            PanelName = string.Empty,
            PanelLongName = string.Empty,
            Enabled = true,
            Grouped = false,
            Tests = new List<MethodPanelTest>()
        });
    }

    protected override Task<PanelsDetailsViewModel> CreateViewModel(MethodPanelDetails model)
    {
        return Task.FromResult(new PanelsDetailsViewModel(model));
    }
}
