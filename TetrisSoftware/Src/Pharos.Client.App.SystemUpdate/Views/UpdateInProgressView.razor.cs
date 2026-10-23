using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.SystemUpdate.Views;

public partial class UpdateInProgressView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
}
