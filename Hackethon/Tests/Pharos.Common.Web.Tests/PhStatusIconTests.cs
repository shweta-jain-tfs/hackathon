using Bunit;
using Pharos.Common.Web.Components.Icons;

namespace Pharos.Common.Web.Tests;

public class PhStatusIconTests : BunitContext
{
    [Fact]
    public void PhStatusIcon_AllEnumValues_ShouldBeRenderable()
    {
        foreach (var icon in Enum.GetValues<StatusIconType>())
        {
            Render<PhStatusIcon>(p => p.Add(c => c.Icon, icon));
        }
    }

    [Fact]
    public void PhStatusIcon_InvalidEnumValue_ShouldThrow()
    {
        const StatusIconType notExistingValue = (StatusIconType)1000;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Render<PhStatusIcon>(p => p.Add(c => c.Icon, notExistingValue)));
    }
}