using Pharos.Client.App.Parameters.Reflex.Models;
using Pharos.Client.App.Parameters.Reflex.ViewModels;
using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Client.App.Parameters.Reflex.Views;

public partial class HeaderView : ComponentBase
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IReflexDetailDataService DataService { get; set; } = default!;
    [Parameter, EditorRequired] public ReflexDefinitionDetailsViewModel ViewModel { get; set; } = default!;

    private const int _maxNameLength = 64;
    private PhTextBox? _reflexNameFieldRef;
    private bool _isValidating;

    private bool IsError => !ViewModel.IsUniqueDefinitionName;
    
    private string ErrorText
    {
        get
        {
            if (ViewModel.IsUniqueDefinitionName)
            {
                return "";
            }

            return string.IsNullOrWhiteSpace(ViewModel.DefinitionName)
                ? Loc["ReflexDetailPage_ValueRequired"]
                : Loc["ReflexDetailPage_DefinitionNameMustBeUnique"];
        }
    }

    // Validate against server when field loses focus
    private async Task OnBlur()
    {
        if (_isValidating)
        {
            return;
        }

        try
        {
            _isValidating = true;
            await ViewModel.ValidateDefinitionNameAsync(DataService);
            StateHasChanged();
        }
        catch
        {
            // Ignore - server will validate on Save anyway
        }
        finally
        {
            _isValidating = false;
        }
    }

    private async Task SetFocusWhenNewEntity()
    {
        if (ViewModel.IsNewEntity &&
            string.IsNullOrWhiteSpace(ViewModel.DefinitionName) &&
            _reflexNameFieldRef is not null)
        {
            await _reflexNameFieldRef.FocusAsync();
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        _ = SetFocusWhenNewEntity();
    }

    public void DiscardChanges()
    {
        _ = SetFocusWhenNewEntity();
    }
}