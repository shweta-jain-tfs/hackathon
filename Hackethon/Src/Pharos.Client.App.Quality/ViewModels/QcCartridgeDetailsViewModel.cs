using Pharos.Client.App.Quality.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.Quality.ViewModels;

/// <summary>
/// ViewModel for displaying QC cartridge information from data matrix barcode.
/// </summary>
public class QcCartridgeDetailsViewModel : LoadingViewModel
{
    private const string _greaterThanSymbol = ">";
    private const string _greaterThanOrEqualSymbol = "≥";
    private const string _lessThanSymbol = "<";
    private const string _lessThanOrEqualSymbol = "≤";
    private const string _rangeSeparator = " – ";

    private readonly IQualityDataSource _dataSource;

    public QcCartridgeDetailsViewModel(IQualityDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public string Barcode { get; set; } = string.Empty;
    public QcCartridgeInformation? QcCartridgeInfo { get; private set; }
    public ICollection<QcCartridgeTestInfo> Tests => QcCartridgeInfo?.Tests ?? [];
    public event Action? OnShowToast;
    public async Task LoadQcDataAsync(string dataMatrix)
    {
        Barcode = dataMatrix;
        await LoadAsync();
    }

    public static string GetAcceptanceRangeText(ICollection<QcCartridgeTestLimit> limits)
    {
        if (limits == null || limits.Count == 0)
        {
            return string.Empty;
        }

        double? lower = null;
        double? upper = null;
        var lowerSymbol = _greaterThanOrEqualSymbol;
        var upperSymbol = _lessThanOrEqualSymbol;

        foreach (var limit in limits)
        {
            switch (limit.LimitType)
            {
                case QcCartridgeLimitType.GreaterThan:
                    lower = limit.LimitValue;
                    lowerSymbol = _greaterThanSymbol;
                    break;
                case QcCartridgeLimitType.GreaterThanOrEqual:
                    lower = limit.LimitValue;
                    lowerSymbol = _greaterThanOrEqualSymbol;
                    break;
                case QcCartridgeLimitType.LessThan:
                    upper = limit.LimitValue;
                    upperSymbol = _lessThanSymbol;
                    break;
                case QcCartridgeLimitType.LessThanOrEqual:
                    upper = limit.LimitValue;
                    upperSymbol = _lessThanOrEqualSymbol;
                    break;
            }
        }

        if (lower.HasValue && upper.HasValue)
        {
            return $"{lower.Value:F1}{_rangeSeparator}{upper.Value:F1}";
        }
        else if (lower.HasValue)
        {
            return $"{lowerSymbol}{lower.Value:F1}";
        }
        else if (upper.HasValue)
        {
            return $"{upperSymbol}{upper.Value:F1}";
        }

        return string.Empty;
    }

    protected override async Task DoLoadAsync()
    {
        var result = await _dataSource.GetQcCartridgeInfo(Barcode);

        if (result != null)
        {
            QcCartridgeInfo = result;

            if (result.IsNewlyRegistered)
            {
                OnShowToast?.Invoke();
            }
        }
    }
}
