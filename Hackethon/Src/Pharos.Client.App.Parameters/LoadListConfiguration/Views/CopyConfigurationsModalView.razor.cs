using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.LoadListConfiguration.Views;

public partial class CopyConfigurationsModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public string SelectedInstrument { get; set; } = default!;
    [Parameter] public Dictionary<string, string>? AvailableInstruments { get; set; }

    private readonly HashSet<string> _selectedInstruments = new();

    private bool IsInstrumentSelected(string instrument)
    {
        return _selectedInstruments.Contains(instrument);
    }

    private void OnInstrumentCheckChanged(string instrument, bool isChecked)
    {
        if (isChecked)
        {
            _selectedInstruments.Add(instrument);
        }
        else
        {
            _selectedInstruments.Remove(instrument);
        }
    }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}