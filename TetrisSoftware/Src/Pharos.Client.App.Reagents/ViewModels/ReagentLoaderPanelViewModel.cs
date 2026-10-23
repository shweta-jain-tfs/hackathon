using Pharos.Client.App.Reagents.Models;
using Pharos.Client.Common.App.Extensions;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.Reagents.ViewModels;

public class ReagentLoaderPanelViewModel : ViewModelBase
{
    private readonly IReagentDrawerDataSource _reagentDrawerDataSource;

    public event Action? RemoveLegendSelection;

    public ReagentLoaderPanelViewModel(IReagentDrawerDataSource reagentDrawerDataSource)
    {
        _reagentDrawerDataSource = reagentDrawerDataSource;
    }

    public ReagentLoader? SelectedReagentLoader
    {
        get;
        set
        {
            if (field != value)
            {
                var previouslySelectedLoaderPosition = field?.Position;
                field = value;
                OnPropertyChanged(nameof(SelectedReagentLoader));

                if (field != null)
                {
                    if (previouslySelectedLoaderPosition == field.Position)
                    {
                        var selectedCellCompartmentPosition = SelectedCellContent?.CompartmentPosition;
                        var selectedCellPosition = SelectedCellContent?.Position;

                        var compartment = field.Compartments
                            .FirstOrDefault(c => c.Position == selectedCellCompartmentPosition);

                        SelectedCellContent = compartment?.CellContent
                            .FirstOrDefault(cc => cc.Position == selectedCellPosition);

                        SelectedReagentType = compartment?.Type == CompartmentReagentType.Carrier;
                    }
                    else
                    {
                        ClearSelection();
                    }
                }
                else
                {
                    ClearSelection();
                }
            }
        }
    }

    public ReagentContentDetail? ReagentContentDetail
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(ReagentContentDetail));
        }
    }

    public CompartmentCellContent? SelectedCellContent
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(SelectedCellContent));
                if (field != null && !field.IsEmpty)
                {
                    LoadReagentLoaderPanelInfo(field.ReagentBarcode, null);
                }
                else
                {
                    ClearSelection();
                }
            }
        }
    }

    public string Location
    {
        get => SelectedReagentLoader?.Location ?? string.Empty;
        set
        {
            if (SelectedReagentLoader != null)
            {
                SelectedReagentLoader.Location = value;
            }
            OnPropertyChanged(nameof(Location));
        }
    }

    public string Name
    {
        get => ReagentContentDetail?.Name ?? string.Empty;
        set
        {
            if (ReagentContentDetail != null)
            {
                ReagentContentDetail.Name = value;
            }
            OnPropertyChanged(nameof(Name));
        }
    }

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

    public string Status
    {
        get => SelectedCellContent?.Status.ToLocalizedString("Reagent") ?? string.Empty;
        set
        {
            if (SelectedCellContent != null)
            {
                SelectedCellContent.Status = Enum.Parse<ReagentPositionStatus>(value);
            }
            OnPropertyChanged(nameof(Status));
        }
    }

    public string Quantity
    {
        get
        {
            if (ReagentContentDetail != null && ReagentContentDetail.Quantity != 0)
            {
                var unit = ReagentContentDetail.UnitOfMeasure.ToLocalizedString("Reagent");
                return $"{ReagentContentDetail.Quantity} {unit}";
            }

            return string.Empty;
        }
        set
        {
            if (ReagentContentDetail != null)
            {
                OnPropertyChanged(nameof(Quantity));
            }
        }
    }

    public string ExpiryDate
    {
        get => ReagentContentDetail?.ExpiryDate?.ToShortDateString() ?? string.Empty;
        set
        {
            if (ReagentContentDetail != null)
            {
                ReagentContentDetail.ExpiryDate = DateTime.Parse(value);
            }
            OnPropertyChanged(nameof(ExpiryDate));
        }
    }

    public string LotNumber
    {
        get => ReagentContentDetail?.LotNumber ?? string.Empty;
        set
        {
            if (ReagentContentDetail != null)
            {
                ReagentContentDetail.LotNumber = value;
            }
            OnPropertyChanged(nameof(LotNumber));
        }
    }

    public bool SelectedReagentType
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedReagentType));
        }
    }

    public ReagentPositionStatus? SelectedLegendStatus
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedLegendStatus));
        }
    }

    public void ResetLoader()
    {
        SelectedReagentLoader = null;
        ClearSelection();
    }

    public void ClearSelection()
    {
        SelectedCellContent = null;
        ReagentContentDetail = null;
        SelectedReagentType = false;
    }

    private static ReagentLoader InitEmptyLoader()
    {
        return new ReagentLoader
        {
            Compartments = Enumerable.Range(1, 12).Select(i => new ReagentCompartment
            {
                Position = i,
                Type = CompartmentReagentType.Carrier,
                CellContent = Enumerable.Range(1, 6).Select(j => new CompartmentCellContent
                {
                    Position = j,
                    IsEmpty = true
                }).ToList()
            }).ToList(),
            IsEmpty = true
        };
    }

    internal void UpdateLoaderPanel(ReagentLoader? loader)
    {
        if (loader != null && !loader.IsEmpty)
        {
            SelectedReagentLoader = loader;
        }
        else if (loader != null && SelectedReagentLoader != null)
        {
            var emptyDefaultLoader = InitEmptyLoader();
            emptyDefaultLoader.Position = loader.Position;
            emptyDefaultLoader.Location = loader.Location;
            SelectedReagentLoader = emptyDefaultLoader;
        }
    }

    internal void UpdateLegendStatus(ReagentPositionStatus? selectedLegendStatus)
    {
        SelectedLegendStatus = selectedLegendStatus;
    }

    private async void LoadReagentLoaderPanelInfo(string? reagentBarcode, string? reagentUdi)
    {
        ReagentContentDetail = await _reagentDrawerDataSource.GetReagentContentDetailAsync(reagentBarcode, reagentUdi);
    }

    internal void InvokeClearLegendSelction()
    {
        RemoveLegendSelection?.Invoke();
    }
}
