using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Parameters.Methods.Views;

public partial class AddLaboratoryNameModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public List<string> CurrentLabNames { get; set; } = default!;

    private readonly MethodTestLabName _newLabName = new();
    private string HelpText
    {
        get
        {
            var testName = _newLabName.TestName;
            if (IsAnyUsernameCharacterInvalid())
            {
                return Loc["LaboratoryName_AddDialog_InvalidCharacter"];
            }
            // Use HashSet for faster lookup if CurrentLabNames is large
            if (!string.IsNullOrEmpty(testName) && CurrentLabNames != null && CurrentLabNames.Count > 10)
            {
                var namesSet = new HashSet<string>(CurrentLabNames);
                if (namesSet.Contains(testName))
                {
                    return Loc["LaboratoryName_AddDialog_TestNameExists"];
                }
            }
            else if (!string.IsNullOrEmpty(testName) && CurrentLabNames != null && CurrentLabNames.Contains(testName))
            {
                return Loc["LaboratoryName_AddDialog_TestNameExists"];
            }
            return "";
        }
    }
    
    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
    private bool IsAnyUsernameCharacterInvalid()
    {
        var labName = _newLabName.TestName;
        if (string.IsNullOrEmpty(labName))
        {
            return false;
        }
        // A HashSet provides very fast lookups (O(1) time complexity) to check if a character is allowed
        var allowedChars = new HashSet<char>(LaboratoryTest.AllowedNameCharacters);
        foreach (var c in labName)
        {
            if (!allowedChars.Contains(c))
            {
                return true;
            }
        }

        return false;
    }
    private bool IsLaboratoryNameInvalid()
    {
        return CurrentLabNames.Contains(_newLabName.TestName) || string.IsNullOrEmpty(_newLabName.TestName) || IsAnyUsernameCharacterInvalid();
    }
}
