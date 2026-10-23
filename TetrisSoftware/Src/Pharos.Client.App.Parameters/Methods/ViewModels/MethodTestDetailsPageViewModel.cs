using Pharos.Client.App.Parameters.Methods.Models;

namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public class MethodTestDetailsPageViewModel : DetailsPageViewModel<MethodTestDetailsViewModel, MethodTestDetails>
{
    public MethodTestDetailsPageViewModel(IMethodTestDetailsPageDataService dataService) : base(dataService, null)
    {
    }

    protected override Task<MethodTestDetails> CreateDefaultModel()
    {
        throw new NotImplementedException("Creation of new MethodsTests is not supported.");
    }

    protected override Task<MethodTestDetailsViewModel> CreateViewModel(MethodTestDetails model)
    {
        return Task.FromResult(new MethodTestDetailsViewModel(model));
    }
}
