using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.ViewModels.RequestGenerator;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.ServiceTools.Views.RequestGenerator;

public partial class PreviewView
{
    [Parameter]
    public GeneratorViewModel ViewModel { get; set; } = null!;

    [Inject]
    public IStringLocalizer<Translations> Loc { get; set; } = default!;
}
