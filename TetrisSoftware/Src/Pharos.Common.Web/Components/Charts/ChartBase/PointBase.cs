using System.Globalization;

namespace Pharos.Common.Web.Components.Charts.ChartBase;

public static class Utilities
{
    // Helper function for invariant double to string conversion
    public static string ToInvariantString(this double value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}

// Point status, for drawing different shapes and colors
public enum PointStatus
{
    normal,
    warning,
    alert
}

public class PointBase
{
    public Object? PointRef { get; set; } // will be used for displaying point data
    public bool Selected { get; set; } // a selected point is displayed with a circle
    public PointStatus Status { get; set; } // status of the point (normal, warning, alert)
}
