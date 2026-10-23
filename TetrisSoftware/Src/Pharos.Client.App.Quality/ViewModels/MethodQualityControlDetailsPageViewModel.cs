using Pharos.Client.App.Quality.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.Quality.ViewModels;

public class MethodQualityControlDetailsPageViewModel(IMethodQualityControlDetailsDataSource dataService)
    : DetailsPageViewModel<MethodQualityControlDetailsViewModel, QualityControl>(dataService, null)
{
    protected override Task<QualityControl> CreateDefaultModel()
    {
        throw new NotImplementedException("Creation of new Method Quality Controls is not supported.");
    }

    protected override Task<MethodQualityControlDetailsViewModel> CreateViewModel(QualityControl model)
    {
        return Task.FromResult(new MethodQualityControlDetailsViewModel(model, dataService));
    }
}
