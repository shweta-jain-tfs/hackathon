using Pharos.Client.App.Parameters.QualityControl.Models;

namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public class LabQualityControlDetailsPageViewModel : DetailsPageViewModel<LabQualityControlDetailViewModel, LabQualityControlDetail>
{
    private readonly IEntityDataSource<TubeDefinition> _tubeDefinitionDataSource;
    private readonly IQualityControlBarcodeFormatItemSource _barcodeFormatDataSource;
    private readonly IStringLocalizer<Translations> _loc;
    private readonly IQualityControlBarcodeFormatNotificationProvider _barcodeFormatNotificationProvider;

    public LabQualityControlDetailsPageViewModel(ILabQualityControlItemSource dataService,
        IEntityDataSource<TubeDefinition> tubeDefinitionDataSource,
        IQualityControlBarcodeFormatItemSource barcodeFormatDataSource,
        IStringLocalizer<Translations> loc,
        IQualityControlBarcodeFormatNotificationProvider barcodeFormatNotificationProvider) : base(dataService, null)
    {
        _tubeDefinitionDataSource = tubeDefinitionDataSource;
        _barcodeFormatDataSource = barcodeFormatDataSource;
        _loc = loc;
        _barcodeFormatNotificationProvider = barcodeFormatNotificationProvider;
    }

    protected override void OnSubscribeToNotifications()
    {
        _barcodeFormatNotificationProvider.RefreshNeeded += OnBarcodeFormatDataChanged;
    }

    protected override void OnUnsubscribeFromNotifications()
    {
        _barcodeFormatNotificationProvider.RefreshNeeded -= OnBarcodeFormatDataChanged;
    }

    private async void OnBarcodeFormatDataChanged(object? sender, EventArgs e)
    {
        if (CurrentDetailsViewModel == null)
        {
            return;
        }

        var barcodeDefinitions = await _barcodeFormatDataSource.GetAll();
        CurrentDetailsViewModel.SetBarcodeFormats(barcodeDefinitions);
    }

    protected override Task<LabQualityControlDetail> CreateDefaultModel()
    {
        return Task.FromResult(new LabQualityControlDetail
        {
            Id = 0,
            Name = string.Empty,
            IdOnBarcode = string.Empty,
            BarcodeFormatId = null,
            TubeTypeId = 0,
            LabQualityControlTests = new List<LabQualityControlTest>()
        });
    }

    protected override async Task<LabQualityControlDetailViewModel> CreateViewModel(LabQualityControlDetail model)
    {
        var tubeDefinitionsTask = _tubeDefinitionDataSource.GetAsync(new DataSourceRequest());
        var barcodeDefinitionsTask = _barcodeFormatDataSource.GetAll();

        await Task.WhenAll(tubeDefinitionsTask, barcodeDefinitionsTask);

        return new LabQualityControlDetailViewModel(model, tubeDefinitionsTask.Result.Items, barcodeDefinitionsTask.Result, _loc);
    }
}