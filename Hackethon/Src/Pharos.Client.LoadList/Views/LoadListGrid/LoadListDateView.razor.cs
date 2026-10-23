using System.Globalization;

namespace Pharos.Client.App.LoadList.Views.LoadListGrid;

/// <summary>
/// Formats a date to show the date without time with the current culture.
/// A fallback text can be set, that will be shown when date is null.
/// </summary>
public partial class LoadListDateView
{
    [Parameter] public DateTimeOffset? DateToShow { get; set; }
    [Parameter] public string TextFallback { get; set; } = string.Empty;
   
    private CultureInfo _culture = CultureInfo.CurrentCulture;

    private string Text { get; set; } = string.Empty;

    protected override Task OnParametersSetAsync()
    {
        _culture = CultureInfo.CurrentCulture;

        Text = DateToShow == null ? TextFallback : DateToShow.Value.ToLocalTime().ToString("d", _culture);

        return base.OnParametersSetAsync();
    }
}