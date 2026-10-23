using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.Reagents.ViewModels.BottleDrawer;

public class BottleDrawerPanelViewModel : ViewModelBase
{
    public bool IsLoading
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }
    }

    public ICollection<string> Colors { get; set; } = new List<string>();

    public ArticleType Type { get; set; }

    public BottlePositionSize Size { get; set; }

    public BottleDetail? SelectedBottle
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(SelectedBottle));
            }
        }
    }

    public string LoadingIndicatorPosition
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(LoadingIndicatorPosition));
            }
        }
    } = string.Empty;

    public event Func<Task>? OnClosePanel;

    public string GetStatus()
    {
        if(SelectedBottle != null && !IsLoading )
        {
            return SelectedBottle.Status.ToLocalizedString("Reagent");
        }

        return string.Empty;
    }

    public string GetQuantity()
    {
        if (SelectedBottle != null)
        {
            var unit = SelectedBottle.UnitOfMeasure.ToLocalizedString("Reagent");
            return $"{SelectedBottle.Quantity} {unit}";
        }

        return string.Empty;
    }

    public string GetExpiryDate()
    {
        return SelectedBottle?.ExpiryDate?.Date.ToShortDateString() ?? string.Empty;
    }

    public void ClearSelection()
    {
        SelectedBottle = null;
    }

    public void ClosePanel()
    {
        OnClosePanel?.Invoke();
    }

    public void StartLoadingSequence(LoadingPosition loadingPosition)
    {
        IsLoading = true;
        UpdatePanelForLoading(loadingPosition);
    }

    public void StopLoadingSequence()
    {
        IsLoading = false;
        LoadingIndicatorPosition = string.Empty;
        ClearPanel();
        ClosePanel();
    }

    public void BottleSelectedWhileLoading()
    {
        IsLoading = false;
        LoadingIndicatorPosition = string.Empty;
    }

    private void UpdatePanelForLoading(LoadingPosition loadingPosition)
    {
        Colors = loadingPosition.Colors;
        Type = loadingPosition.ArticleType;
        Size = loadingPosition.Size;
        SelectedBottle = new BottleDetail
        {
            Id = loadingPosition.Id,
            Name = loadingPosition.Name,
            LotNumber = loadingPosition.LotNumber,
            ExpiryDate = loadingPosition.ExpiryDate,
            Quantity = loadingPosition.Quantity,
            UnitOfMeasure = loadingPosition.UnitOfMeasure,
            Position = loadingPosition.Position,
            Location = string.Empty
        };
    }

    private void ClearPanel()
    {
        Colors.Clear();
        SelectedBottle = null;
    }
}
