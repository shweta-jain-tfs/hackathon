using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Reagents.Views.SolidWasteDrawer;

public partial class SolidWasteIndicatorView : ComponentBase
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
}
