namespace Pharos.Server.Instrument.Application.TestHelpers;
internal class ObjectValidationHelper
{
    public static bool CheckIfObjectContainsString(object? obj, string str)
    {
        if (obj == null)
        {
            return false;
        }

        return obj.ToString()?.Contains(str, StringComparison.OrdinalIgnoreCase) == true;
    }
}
