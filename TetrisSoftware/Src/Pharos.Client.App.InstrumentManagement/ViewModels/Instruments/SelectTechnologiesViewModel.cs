using Pharos.Client.App.InstrumentManagement.Models.Instruments;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.Instruments;

public sealed class SelectTechnologiesViewModel(
    int instrumentId,
    IInstrumentTechnologyDataService dataService) : LoadingViewModel
{
    public List<InstrumentTechnology> Technologies
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public bool IsModified
    {
        get;
        private set => SetField(ref field, value);
    }

    protected override async Task DoLoadAsync()
    {
        Technologies = await dataService.GetAsync(instrumentId);
        IsModified = false;
    }

    public void SetSelected(InstrumentTechnology technology, bool isSelected)
    {
        if (technology.IsSelected == isSelected)
        {
            return;
        }

        technology.IsSelected = isSelected;
        IsModified = true;
    }

    public async Task<bool> SaveAsync()
    {
        if (IsBusy)
        {
            return false;
        }

        IsBusy = true;
        try
        {
            await dataService.UpdateAsync(instrumentId, Technologies);
            IsModified = false;
            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
