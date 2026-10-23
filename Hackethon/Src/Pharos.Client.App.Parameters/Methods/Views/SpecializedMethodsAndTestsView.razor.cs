using Pharos.Client.App.Parameters.Methods.ViewModels;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Methods.Views;

public partial class SpecializedMethodsAndTestsView : IDisposable
{
    private Dictionary<string, string> _typeList = default!;

    [Inject] private SpecializedMethodsAndTestsViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    protected override void OnInitialized()
    {
        _typeList = Enum.GetValues<SpecializedMethodOrTestType>()
            .ToDictionary(type => type.ToString(), type => type.ToLocalizedString());
    }

    private async Task Save()
    {
        await ViewModel.Save(CancellationToken.None);
        await Modal.CloseAsync(ModalResult.Ok(true));
    }

    private Task CloseModal(ModalResult result)
    {
        return Modal.CloseAsync(result);
    }

    private string GetTypeDisplay(SpecializedMethodOrTestType type)
    {
        return _typeList[type.ToString()];
    }

    public void Dispose()
    {
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
