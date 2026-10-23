using Bunit;
using Pharos.Common.Web.Components.Icons;

namespace Pharos.Common.Web.Tests;

public class PhIconTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void PhIcon_AllEnumValues_ShouldHaveMatchingSvgComponent()
    {
        using var ctx = new BunitContext();

        foreach (var icon in Enum.GetValues<IconType>())
        {
            try
            {
                var component = ctx.Render<PhIcon>(
                    parameters => parameters.Add(c => c.IconType, icon)
                );
            }
            catch (Exception)
            {
                testOutputHelper.WriteLine($"Enum {icon} expects component {icon}.razor to exist.");
                Assert.True(false);
            }
        }
    }

    [Fact]
    public void PhIcon_ShouldNotContainHardCodedColorCodes()
    {
        using var ctx = new BunitContext();

        foreach (var icon in Enum.GetValues<IconType>())
        {
            var component = ctx.Render<PhIcon>(
                parameters => parameters.Add(c => c.IconType, icon)
            );

            component.Markup.Should().NotContainAny(["fill=\"#", "stroke=\"#"], $"The icon \"{icon}\" should not contain hard coded colors");
        }
    }

    [Fact]
    public void PhIcon_MissingSvgFile_ShouldThrowException()
    {
        const IconType notExistingValue = (IconType)1000;

        using var ctx = new BunitContext();

        Assert.Throws<InvalidOperationException>(() => ctx.Render<PhIcon>(
            parameters => parameters.Add(c => c.IconType, notExistingValue))
        );
    }
}