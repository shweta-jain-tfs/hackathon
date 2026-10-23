using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Reagents.Views.LiquidDrawer;
public partial class LiquidIndicatorView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    //TODO: Placeholder for Liquid Indicator View for future implementation
}
