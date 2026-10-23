using Microsoft.Extensions.Localization;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Common.Web.Components.TabControl;

namespace Pharos.Client.App.Result.Views.Result;

public partial class TestReplicateModalView
{
    [Parameter] public List<TestReplicateDetail> Replicates { get; set; } = default!;
    [Parameter] public List<DropDownOption<int>> DropDownList { get; set; } = default!;
    [Parameter] public int PreSelectedReplicate { get; set; }
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] public IAuthorizationService AuthorizationService { get; set; } = null!;

    private bool IsRndMode { get; set; }

    private int SelectedReplicate
    {
        get;
        set
        {
            field = value;
            StateHasChanged();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        IsRndMode = await AuthorizationService.IsRndMode();
        SelectedReplicate = PreSelectedReplicate;
    }

    private StatusType GetStatus(DateTimeOffset contextExpiryDate)
    {
        var timeOfUse = Replicates.Single(x => x.Id == SelectedReplicate).MeasurementDateTime;
        return contextExpiryDate < timeOfUse ? StatusType.Alert : StatusType.None;
    }

    private static string GetDateAsString(DateTimeOffset contextExpiryDate)
    {
        return contextExpiryDate.ToShortDateString();
    }

    private TabControlSymbolType GetHeaderSymbol()
    {
        var replicate = Replicates.Single(x => x.Id == SelectedReplicate);

        return replicate.Reagents.Select(r => GetStatus(r.ExpiryDate)).Any(status => status != StatusType.None)
            ? TabControlSymbolType.Alert : TabControlSymbolType.None;
    }
}