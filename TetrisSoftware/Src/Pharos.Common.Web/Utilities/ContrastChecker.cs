namespace Pharos.Common.Web.Utilities;

/// <remarks>
/// Color strings must be opaque hexadecimal values in <c>#RRGGBB</c> or
/// <c>RRGGBB</c> format.
/// </remarks>
public static class ContrastChecker
{
    public const double WcagAaNonTextMinimumContrastRatio = 3.0;

    private const string _darkBackgroundHex = "#1B1B1D"; // Secondary layer
    private const string _lightBackgroundHex = "#F5F5F5"; // Provisional light surface
    private const string _printBackgroundHex = "#FFFFFF"; // Plain print surface
    private static readonly RgbColor _darkBackgroundColor = ParseHexColor(_darkBackgroundHex);
    private static readonly RgbColor _lightBackgroundColor = ParseHexColor(_lightBackgroundHex);
    private static readonly RgbColor _printBackgroundColor = ParseHexColor(_printBackgroundHex);

    public static bool AnyColorBelowMinimumContrastRatio(
        IEnumerable<string?> colors,
        ContrastSurface surface,
        double minimumContrastRatio)
    {
        var background = GetBackgroundColor(surface);

        return colors
            .OfType<string>()
            .Where(color => !string.IsNullOrWhiteSpace(color))
            .Select(ParseHexColor)
            .Any(graphic => GetContrastRatio(graphic, background) < minimumContrastRatio);
    }

    public static bool IsColorBelowMinimumContrastRatio(
        string? color,
        ContrastSurface surface,
        double minimumContrastRatio)
    {
        return AnyColorBelowMinimumContrastRatio([color], surface, minimumContrastRatio);
    }

    internal static double GetContrastRatio(
        string firstColor,
        string secondColor)
    {
        return GetContrastRatio(ParseHexColor(firstColor), ParseHexColor(secondColor));
    }

    // WCAG 2.2 contrast ratio:
    // https://www.w3.org/TR/WCAG22/#dfn-contrast-ratio
    private static double GetContrastRatio(
        RgbColor graphic,
        RgbColor background)
    {
        var graphicLuminance = GetRelativeLuminance(graphic);
        var backgroundLuminance = GetRelativeLuminance(background);

        var lighter = Math.Max(graphicLuminance, backgroundLuminance);
        var darker = Math.Min(graphicLuminance, backgroundLuminance);

        return (lighter + 0.05) / (darker + 0.05);
    }

    // WCAG 2.2 relative luminance for sRGB.
    // The transfer-function constants and RGB weights are defined by WCAG:
    // https://www.w3.org/TR/WCAG22/#dfn-relative-luminance
    private static double GetRelativeLuminance(RgbColor color)
    {
        var red = ToLinearSrgb(color.Red);
        var green = ToLinearSrgb(color.Green);
        var blue = ToLinearSrgb(color.Blue);

        return (0.2126 * red) + (0.7152 * green) + (0.0722 * blue);
    }

    private static double ToLinearSrgb(byte channel)
    {
        var value = channel / 255.0;

        return value <= 0.04045
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static RgbColor ParseHexColor(string hexColor)
    {
        var hex = hexColor.Trim().TrimStart('#');
        var bytes = Convert.FromHexString(hex);

        if (bytes.Length != 3)
        {
            throw new FormatException(
                $"'{hexColor}' is not a valid #RRGGBB color.");
        }

        return new RgbColor(bytes[0], bytes[1], bytes[2]);
    }

    private static RgbColor GetBackgroundColor(ContrastSurface surface)
    {
        return surface switch
        {
            ContrastSurface.Dark => _darkBackgroundColor,
            ContrastSurface.Light => _lightBackgroundColor,
            ContrastSurface.Print => _printBackgroundColor,
            _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, null)
        };
    }

    private readonly record struct RgbColor(byte Red, byte Green, byte Blue);
}


public enum ContrastSurface
{
    Dark,
    Light,
    Print
}