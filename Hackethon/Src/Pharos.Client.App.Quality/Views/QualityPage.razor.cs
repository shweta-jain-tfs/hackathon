using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Quality.Views;

public partial class QualityPage
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
}
