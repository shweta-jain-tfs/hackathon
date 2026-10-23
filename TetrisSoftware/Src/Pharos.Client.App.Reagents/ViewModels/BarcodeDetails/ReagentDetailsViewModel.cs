using Pharos.Client.App.Reagents.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.Reagents.ViewModels.BarcodeDetails;

public class ReagentDetailsViewModel : LoadingViewModel
{
    private readonly IReagentDrawerDataSource _dataSource;

    public string Barcode { get; set; } = string.Empty;
    public ReagentContentDetail? ReagentContentDetail { get; private set; }
    public string? ToastMessage { get; private set; }
    public string Type
    {
        get => ReagentContentDetail?.Type?.ToLocalizedString() ?? string.Empty;
        set
        {
            if (ReagentContentDetail != null && Enum.TryParse<ArticleType>(value, out var parsedType))
            {
                ReagentContentDetail.Type = parsedType;
            }
            OnPropertyChanged(nameof(Type));
        }
    }

    public ReagentDetailsViewModel(IReagentDrawerDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task LoadReagentDetailsAsync(string barcode)
    {
        Barcode = barcode;
        await LoadAsync();
    }

    protected override async Task DoLoadAsync()
    {
        var result = await _dataSource.GetReagentContentDetailAsync(String.Empty, Barcode);

        ReagentContentDetail = result;
    }
}
