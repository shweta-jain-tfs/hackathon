namespace Pharos.Common.Web.Extensions;

public static class EnumExtension
{
    public static string TypeNameToString(this Enum val) { return val.ToString().ToLower(); }
}