using Pharos.Client.App.Quality.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Client.App.Quality.ViewModels;

internal class MethodViewModel(IMethodDataSource dataSource) : LoadingViewModel
{
    public string InstrumentNumber { get; set; } = default!;
    public string MethodName { get; set; } = default!;
    public string InstrumentName { get; set; } = string.Empty;
    public string ActiveConjugateLots { get; set; } = "-";

    public int SelectedDays
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedDays));
        }
    } = 30;

#pragma warning disable CA1822
    public List<DropDownOption<int>> DataForDays =>
#pragma warning restore CA1822
    [
        new() { Value = 1, Text = "1" }, // TODO: Remove, just for test
        new() { Value = 2, Text = "2" }, // TODO: Remove, just for test
        new() { Value = 3, Text = "3" }, // TODO: Remove, just for test
        new() { Value = 10, Text = "10" },
        new() { Value = 20, Text = "20" },
        new() { Value = 30, Text = "30" }
    ];

    protected override async Task DoLoadAsync()
    {
        var conjugateLotList = await dataSource.GetActiveConjugateLots(InstrumentNumber, MethodName);

        if (conjugateLotList is { Count: > 0 })
        {
            ActiveConjugateLots = string.Join(", ", conjugateLotList.ToList());
        }
    }
}
