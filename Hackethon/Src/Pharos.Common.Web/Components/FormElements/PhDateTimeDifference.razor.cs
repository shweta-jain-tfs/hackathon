namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// Returns the reader friendly date difference between a starting point and an earlier date. 
/// A fallback text can be set, that will be shown when date is null.
/// </summary>
public partial class PhDateTimeDifference : ComponentBase
{
    [Parameter] public DateTimeOffset? DateTimeDifferencePoint { get; set; }
    [Parameter] public DateTimeOffset? DateTimeStartPoint { get; set; }
    [Parameter] public string TextFallback { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    protected override Task OnParametersSetAsync()
    {
        if (DateTimeDifferencePoint == null || DateTimeStartPoint == null)
        {
            Text = TextFallback;
        }
        else
        {
            if (DateTimeDifferencePoint > DateTimeOffset.Now)
            {
                Text = "Future time";
            }
            else
            {
                var startPoint = DateTimeStartPoint.Value;
                var timeDifference = startPoint - DateTimeDifferencePoint.Value;
                // TODO: add localization
                if (Math.Floor(timeDifference.TotalDays) > 0)
                {
                    Text = timeDifference.TotalDays switch
                    {
                        < 30 => "Less than 30 days ago",
                        < 365 => "Less than 1 year ago",
                        _ => $"{Math.Floor(timeDifference.TotalDays / 365)} year(s) ago"
                    };
                }
                else
                {
                    Text = timeDifference.TotalMinutes switch
                    {
                        < 1 => "Just now",
                        < 60 => "Less than 1 hour ago",
                        _ => "Less than 24 hours ago",
                    };
                }
            }
        }

        return base.OnParametersSetAsync();
    }
}
