namespace Pharos.Client.Common.Models.Tests;

public class LocalizedPropertiesTest
{
    [Theory]
    [InlineData(typeof(TestOrderState))]
    public void AllEnumValuesHaveResourceStrings(Type enumType)
    {
        var enumValues = Enum.GetValues(enumType);
        foreach (var enumValue in enumValues)
        {
            ((Enum)enumValue).ToLocalizedString("", throwIfNotFound: true);
        }
    }
}