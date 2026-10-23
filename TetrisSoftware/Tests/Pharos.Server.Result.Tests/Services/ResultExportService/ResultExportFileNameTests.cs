using Pharos.Server.Result.Application.Services.ResultExportService;

namespace Pharos.Server.Result.Services.ResultExportService;

public class ResultExportFileNameTests
{
    [Fact]
    [Trait("ModuleRequirement", "MR_3678")]
    public void GenerateFileName_ShouldIncludeMethodNameForSingleAnalyticalRun()
    {
        var fileName = ResultExportFileName.GenerateFileName(DateTime.Now, "inst", "", "method");

        fileName.Should().Contain("method");
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5688")]
    [Trait("ProductRequirement", "PR_2136")]
    public void GenerateFileName_ShouldExcludeMethodNameForCustomRun()
    {
        var fileName = ResultExportFileName.GenerateFileName(DateTime.Now, "inst", "cmrid", "method");

        fileName.Should().NotContain("method");
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5688")]
    [Trait("ProductRequirement", "PR_2136")]
    public void GenerateFileName_ShouldIncludeCustomRunIdForCustomRun()
    {
        var fileName = ResultExportFileName.GenerateFileName(DateTime.Now, "inst", "cmrid", "method");

        fileName.Should().Contain("cmrid");
    }

    [Theory]
    [InlineData("inst", "cmrid", "method", "inst-cmrid-20240516-131530.csv")]
    [InlineData("inst", "", "method", "inst-method-20240516-131530.csv")]
    [InlineData("inst", "", "", "inst-20240516-131530.csv")]
    [InlineData("", "", "", "result_export-20240516-131530.csv")]
    [InlineData("invalid:characters/in name", "", "", "invalid_characters_in name-20240516-131530.csv")]
    [Trait("ModuleRequirement", "MR_3678")]
    public void GenerateFileName_ShouldMatch(string instrumentName, string customRunId, string methodName, string expectedFileName)
    {
        var startDate = new DateTime(2024, 05, 16, 13, 15, 30);
        var fileName = ResultExportFileName.GenerateFileName(startDate, instrumentName, customRunId, methodName);

        fileName.Should().Match(expectedFileName);
    }
}
