using Pharos.Client.App.Parameters.Methods.Models;

namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public class MethodDetailsPageViewModel : DetailsPageViewModel<MethodDetailsViewModel, MethodDetails>
{
    private readonly ILisChannelDataSource _lisChannelDataSource;

    public MethodDetailsPageViewModel(
        IMethodDataSource dataService,
        ILisChannelDataSource lisChannelDataSource) : base(dataService, null)
    {
        _lisChannelDataSource = lisChannelDataSource;
    }

    protected override Task<MethodDetails> CreateDefaultModel()
    {
        throw new NotImplementedException("Creation of new Methods is not supported.");
    }

    protected override async Task<MethodDetailsViewModel> CreateViewModel(MethodDetails model)
    {
        var channels = await _lisChannelDataSource.GetChannelsAsync();
        return new MethodDetailsViewModel(model, channels);
    }
}
