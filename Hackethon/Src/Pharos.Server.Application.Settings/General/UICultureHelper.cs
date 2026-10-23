namespace Pharos.Server.Application.Settings.General;

public static class UICultureHelper
{
    public static string ToCultureName(UICulture culture)
    {
        return culture.ToString().Replace('_', '-');
    }

    public static string[] GetSupportedCultures()
    {
        return Enum.GetValues<UICulture>()
            .Select(c => c.ToCultureName())
            .ToArray();
    }

    public static string GetDefaultCulture()
    {
        return UICulture.en_US.ToCultureName();
    }
}
