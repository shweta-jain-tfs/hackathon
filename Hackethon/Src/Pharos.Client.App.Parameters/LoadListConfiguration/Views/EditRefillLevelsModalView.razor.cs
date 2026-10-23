using Pharos.Client.App.Parameters.LoadListConfiguration.ViewModels;
using Pharos.Client.Common.App.Validation;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.LoadListConfiguration.Views;
public partial class EditRefillLevelsModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public HashSet<Client.Common.Models.LoadListConfiguration>? SelectedConfigurations { get; set; } = default!;
    [Parameter] public string SelectedInstrument { get; set; } = default!;

    private HashSet<Client.Common.Models.LoadListConfiguration>? _updatedConfigurations = [];
    private PhValidationContext? _validation;
    private ConfigurationItem ConfigurationItem { get; } = new();

    protected override void OnInitialized()
    {
        _updatedConfigurations = SelectedConfigurations != null
            ? [.. SelectedConfigurations]
            : [];

        ConfigurationItem.ConfigurationsToEdit = _updatedConfigurations; 
    }
    
    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _validation?.Validate();
        StateHasChanged();
    }

    private void OnRefillLevelChanged(int? value)
    {
        ConfigurationItem.RefillLevel = value;
        _validation?.Validate();
    }

    private void OnTriggerLevelChanged(int? value)
    {
        ConfigurationItem.TriggerLevel = value;
        _validation?.Validate();
    }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
    
    private async Task ValidateAndSave()
    {
        if (_validation?.Validate() != true)
        {
            return;
        }

        await CloseModal(ModalResult.Ok((_updatedConfigurations?.Select(x => x.Id).ToList(),
            ConfigurationItem.RefillLevel,
            ConfigurationItem.TriggerLevel)));
    }


    private bool IsValid()
    {
        return _validation?.IsValid() ?? false;
    }

    private void RemoveFromSelection(int contextId)
    {
        _updatedConfigurations?.RemoveWhere(x => x.Id == contextId);
        _validation?.Validate();
    }
}
