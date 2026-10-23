using Pharos.Server.Integration.Tests.Common;

namespace Pharos.Server.Parameters.FileComparer;

public class ParameterFileComparerTests
{
    [Trait("ProductRequirement", "PR_1932")]
    [Trait("ModuleRequirement", "MR_3710")]
    [Fact]
    public void InsignificantVariationsAreIgnored()
    {
        var dir = DataDrivenTestHelper.GetTestDataFilesPath(GetType());
        ParameterFileComparer.CheckMethodFilesEqual(Path.Combine(dir, "TestMethod.mth"), Path.Combine(dir, "TestMethodWithVariations.mth"), true, true);
    }

    [Trait("ProductRequirement", "PR_358")]
    [Trait("ModuleRequirement", "MR_4667")]
    [Fact]
    public void ExtraSectionWithEmptyValue()
    {
        var dir = DataDrivenTestHelper.GetTestDataFilesPath(GetType());
        try
        {
            ParameterFileComparer.CheckMethodFilesEqual(Path.Combine(dir, "TestMethod.mth"), Path.Combine(dir, "TestMethodExtraSection.mth"), true, true);
            Assert.Fail();
        }
        catch (InvalidOperationException)
        {
        }
    }
}
