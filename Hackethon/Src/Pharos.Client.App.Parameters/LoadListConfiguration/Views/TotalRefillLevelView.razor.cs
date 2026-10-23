namespace Pharos.Client.App.Parameters.LoadListConfiguration.Views;

public partial class TotalRefillLevelView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Parameter] public RefillLevelInfo? RefillLevelInfo { get; set; }
}