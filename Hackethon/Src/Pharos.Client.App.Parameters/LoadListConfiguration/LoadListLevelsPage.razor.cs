using Pharos.Client.App.Parameters.LoadListConfiguration.ViewModels;
using Pharos.Client.App.Parameters.LoadListConfiguration.Views;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.App.Parameters.LoadListConfiguration;

public partial class LoadListLevelsPage
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private InstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;
    [Inject] internal LoadListConfigurationsViewModel ViewModel { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = null!;
    [Inject] private IToasterService ToasterService { get; set; } = default!;
    protected List<BreadcrumbItem> Breadcrumbs =>
    [
        new(Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
        new(Loc["NavMenu_LoadListLevels"], PageRoute.Parameters.LoadListLevels)
    ];

    private List<DropDownOption<string>> Instruments { get; } = [];
    private string SelectedInstrument { get; set; } = default!; 

    protected override async Task OnInitializedAsync()
    {
        await InstrumentFilterModel.LoadAsync();

        Instruments.AddRange(InstrumentFilterModel.FilterDataSourceSerial
            .Select(instrument => new DropDownOption<string>
            {
                Text = instrument.Value,
                Value = instrument.Key
            }));

        var selectedInstrument
            = Instruments.FirstOrDefault(x => x.Value == InstrumentFilterModel.DefaultFilterValueSerial);

        SelectedInstrument = (selectedInstrument?.Value) ?? string.Empty;
    }

    private string GetDisplayName(string instrument)
    {
        var selectedInstrument
            = Instruments.FirstOrDefault(x => x.Value == instrument);

        var display = (selectedInstrument?.Text) ?? string.Empty;

        return display; 
    }


    internal async Task OpenCopyModal()
    {
        var availableInstruments = InstrumentFilterModel.FilterDataSourceSerial
            .Where(x => x.Key != SelectedInstrument)
            .ToDictionary(x => x.Key, x => x.Value);

        var displayName = GetDisplayName(SelectedInstrument);

        var parameters = new ModalParameters()
        {
            {nameof(CopyConfigurationsModalView.SelectedInstrument), displayName},
            {nameof(CopyConfigurationsModalView.AvailableInstruments), availableInstruments}
        };

        var options = new ModalOptions
        {
            Title = Loc["LoadListCopyConfigurations_Title"],
            ModalWidth = ModalSize.Medium,
            ModalHeight = ModalSize.Large,
            ShowCloseButton = true,
            Status = ModalStatus.Info
        };

        var modal = Modal.Show<CopyConfigurationsModalView>(options, parameters);
        var result = await modal.Result;

        if (result is { Canceled: false, Data: (HashSet<string> selectedInstruments) })
        {
            await ViewModel.CopyLevels(SelectedInstrument, selectedInstruments);

            if (selectedInstruments.Count == 1)
            {
                var instrumentDisplayName = GetDisplayName(selectedInstruments.First());
                ToasterService.ShowToast(ToastType.Confirmation, Loc["LoadListLevels_CopySuccessSingle_Toaster", instrumentDisplayName]);
            }
            else if (selectedInstruments.Count > 1)
            {
                var displayNames = selectedInstruments.Select(GetDisplayName).ToList();
                var joinedInstruments = string.Join(", ", displayNames.SkipLast(1));
                var lastInstrument = displayNames[^1];
                ToasterService.ShowToast(ToastType.Confirmation, Loc["LoadListLevels_CopySuccess_Toaster", joinedInstruments, lastInstrument]);
            }
        }
    }
}