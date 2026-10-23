using Pharos.Client.App.Parameters.QualityControl.Models;

namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public class PhadiaQualityControlDetailsPageViewModel : DetailsPageViewModel<PhadiaQualityControlDetailViewModel, PhadiaQualityControlDetail>
{
    private readonly IEntityDataSource<TubeDefinition> _tubeDefinitionDataSource;

    public PhadiaQualityControlDetailsPageViewModel(IPhadiaQualityControlItemSource dataService, IEntityDataSource<TubeDefinition> tubeDefinitionDataSource) : base(dataService, null)
    {
        _tubeDefinitionDataSource = tubeDefinitionDataSource;
    }

    protected override Task<PhadiaQualityControlDetail> CreateDefaultModel()
    {
        throw new NotImplementedException("Creation of new Phadia Quality Controls is not supported.");
    }

    protected override async Task<PhadiaQualityControlDetailViewModel> CreateViewModel(PhadiaQualityControlDetail model)
    {
        var tubeDefinitions = await _tubeDefinitionDataSource.GetAsync(new DataSourceRequest());

        return new PhadiaQualityControlDetailViewModel(model, tubeDefinitions.Items);
    }
}