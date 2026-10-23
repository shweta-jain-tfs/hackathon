namespace Pharos.Server.Application.Settings.General;

public static class UICultureExtensions
{
    public static string ToCultureName(this UICulture culture)
    {
        return UICultureHelper.ToCultureName(culture);
    }
}
