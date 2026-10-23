using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.DataGrid.Filters.Models;
using Pharos.Common.Web.Components.Status;
using Pharos.Common.Web.Services;

namespace Pharos.Client.App.LoadList.Views.LoadListHeaderView;

public record LoadListFilters(
    Dictionary<string, string> Instruments,
    List<LoadListStatus> Statuses);

public partial class LoadListHeaderView
{
    private const string _componentId = "loadlist_filter";

    [Inject] private InstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IComponentStateService ComponentStateService { get; set; } = default!;
    [Parameter] public int ReagentLoadersCount { get; set; }
    [Parameter] public Func<LoadListStatus, StatusType> StatusLookup { get; set; } = default!;

    private List<SelectableKeyValue> InstrumentList { get; set; } = [];
    private List<SelectableEnum> StatusList { get; set; } = [];

    [Parameter] public EventCallback<LoadListFilters> OnFiltersChanged { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var filterState = await ComponentStateService.GetStateAsync<LoadListFilters>(_componentId);

        //Set the instrument filter with default filter value
        await InstrumentFilterModel.LoadAsync();

        InstrumentList = InstrumentFilterModel.FilterDataSourceSerial
            .Select(x => new SelectableKeyValue(x.Key, x.Value)).ToList();

        StatusList =
        [
            new SelectableEnum((int) LoadListStatus.Alarm, LoadListStatus.Alarm.ToLocalizedString()),
            new SelectableEnum((int) LoadListStatus.Warning, LoadListStatus.Warning.ToLocalizedString()),
            new SelectableEnum((int) LoadListStatus.Pending, LoadListStatus.Pending.ToLocalizedString())
        ];

        if (filterState is null)
        {
            var defaultKey = InstrumentFilterModel.DefaultFilterValueSerial;
            var instrument = InstrumentList.FirstOrDefault(x => x.Key == defaultKey);
            if (instrument != null)
            {
                instrument.IsSelected = true;
                await NotifyFiltersChanged(false);
            }
        }
        else
        {
            foreach (var serial in filterState.Instruments.Keys)
            {
                var instrument = InstrumentList.FirstOrDefault(x => x.Key == serial);
                instrument?.IsSelected = true;
            }

            foreach (var state in filterState.Statuses)
            {
                var status = StatusList.FirstOrDefault(x => x.Value == (int)state);
                status?.IsSelected = true;
            }

            await NotifyFiltersChanged(false);
        }
    }

    private async Task NotifyFiltersChanged(bool persist)
    {
        var filters = GetFilters();

        if(persist)
        {
            await ComponentStateService.SetStateAsync(_componentId, filters);
        }

        await OnFiltersChanged.InvokeAsync(filters);
    }

    private Task NotifyFiltersChanged()
    {
        return NotifyFiltersChanged(true);
    }

    private LoadListFilters GetFilters()
    {
        var instruments = InstrumentList
            .Where(x => x.IsSelected)
            .ToDictionary(x => x.Key, x => x.DisplayName);

        var statuses = StatusList
            .Where(x => x.IsSelected)
            .Select(x => (LoadListStatus)x.Value)
            .ToList();

        return new LoadListFilters(instruments, statuses);
    }

    private async Task ClearInstrumentFilter()
    {
        if (!InstrumentList.Any(x => x.IsSelected))
        {
            return;
        }

        InstrumentList.ForEach(x => x.IsSelected = false);
        await NotifyFiltersChanged();
    }

    private async Task ClearStatusFilter()
    {
        if (!StatusList.Any(x => x.IsSelected))
        {
            return;
        }

        StatusList.ForEach(x => x.IsSelected = false);
        await NotifyFiltersChanged();
    }

    private async Task ClearAllFilters()
    {
        InstrumentList.ForEach(x => x.IsSelected = false);
        StatusList.ForEach(x => x.IsSelected = false);

        await NotifyFiltersChanged();
    }

    private async Task ResetAllFilters()
    {
        var defaultKey = InstrumentFilterModel.DefaultFilterValueSerial;

        InstrumentList.ForEach(x => x.IsSelected = x.Key == defaultKey);
        StatusList.ForEach(x => x.IsSelected = false);

        await NotifyFiltersChanged();
    }

    private StatusType GetIcon(int statusValue)
    {
        var status = (LoadListStatus)statusValue;

        return !Enum.IsDefined(status) ? StatusType.None : StatusLookup(status);
    }
}