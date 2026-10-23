using System.Reflection;
using Pharos.Common.Web.Contexts;

namespace Pharos.Common.Web.Tests;

public class UiComponentStringsDefaultsTests
{
    [Fact]
    public void Default_Should_Not_Contain_Empty_Strings()
    {
        var defaultStrings = UiComponentStrings.Default;

        foreach (var componentStringProp in typeof(UiComponentStrings)
                 .GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var componentStrings = componentStringProp.GetValue(defaultStrings);
            Assert.NotNull(componentStrings);

            foreach (var stringProp in componentStringProp.PropertyType
                     .GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (stringProp.PropertyType == typeof(string))
                {
                    Assert.False(
                        string.IsNullOrWhiteSpace((string?)stringProp.GetValue(componentStrings)),
                        $"{componentStringProp.PropertyType.Name}.{stringProp.Name} is empty in Default.");
                }
            }
        }
    }
}
