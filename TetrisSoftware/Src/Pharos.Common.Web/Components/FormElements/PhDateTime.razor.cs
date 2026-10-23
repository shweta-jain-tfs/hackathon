using System.Globalization;
namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// Formats a date in a common way.
/// A fallback text can be set, that will be shown when date is null.
/// </summary>
public partial class PhDateTime
{
    [Parameter] public DateTimeOffset? DateToShow { get; set; }
    [Parameter] public string TextFallback { get; set; } = string.Empty;
    /// <summary>
    /// Optional forced culture to use instead of the current culture. Used for testing.
    /// </summary>
    [Parameter] public string? Culture { get; set; }

    private CultureInfo _culture = CultureInfo.CurrentCulture;

    private string Text { get; set; } = string.Empty;

    protected override Task OnParametersSetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Culture))
        {
            try
            {
                _culture = CultureInfo.GetCultureInfo(Culture);
            }
            catch (CultureNotFoundException)
            {
                _culture = CultureInfo.CurrentCulture;
            }
        }
        else
        {
            _culture = CultureInfo.CurrentCulture;
        }

        if (DateToShow == null)
        {
            Text = TextFallback;
        }
        else
        {
            Text = DateToShow.Value.Date == DateTime.Today
                ? $"{DateToShow.Value.ToLocalTime().ToString("t", _culture)}"
                : $"{DateToShow.Value.ToLocalTime().ToString("g", _culture)}";
        }

        return base.OnParametersSetAsync();
    }
}