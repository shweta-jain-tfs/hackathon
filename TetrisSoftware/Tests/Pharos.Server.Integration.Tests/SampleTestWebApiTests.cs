using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetTestResults;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class SampleTestWebApiTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    private readonly Fixture _fixture = new();

    [Fact]
    public async Task GetTestResultsShouldReturnSampleAndQualityControlTests()
    {
        var seededResults = await SeedSampleAndQualityControlTests();
        var uri = new Uri(Factory.Server.BaseAddress, $"api/results?AnalyticalRunId={seededResults.AnalyticalRunId}");
        var result = await Factory.HttpClient.GetFromJsonAsync<PartialQueryResult<AnalyticalRunTestResultDto>>(uri, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(2);

        var sample = result.Items.Single(x => x.ResultType == TestResultType.Sample);
        sample.SampleId.Should().Be(seededResults.SampleId);
        sample.MeanConcentrationAsText.Should().Be("50");
        sample.ApprovalStatus.Should().Be(ResultApprovalStatus.Approved);
        sample.TestOrderId.Should().NotBeNull();

        var qualityControl = result.Items.Single(x => x.ResultType == TestResultType.QualityControl);
        qualityControl.SampleId.Should().Be(seededResults.QualityControlName);
        qualityControl.SampleIdDisplay.Should().Be(seededResults.QualityControlName);
        qualityControl.MeanConcentrationAsText.Should().Be("25");
        qualityControl.ApprovalStatus.Should().BeNull();
        qualityControl.TestOrderId.Should().BeNull();
    }

    [Fact]
    public async Task GetTestResultsShouldFilterByTestType()
    {
        var seededResults = await SeedSampleAndQualityControlTests();
        var uri = new Uri(
            Factory.Server.BaseAddress,
            $"api/results?AnalyticalRunId={seededResults.AnalyticalRunId}&ResultTypes={TestResultType.QualityControl}");

        var result = await Factory.HttpClient.GetFromJsonAsync<PartialQueryResult<AnalyticalRunTestResultDto>>(uri, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Single().ResultType.Should().Be(TestResultType.QualityControl);
        result.Items.Single().SampleId.Should().Be(seededResults.QualityControlName);
    }

    [Fact]
    public async Task GetTestResultsShouldSortByTestType()
    {
        var seededResults = await SeedSampleAndQualityControlTests();
        var uri = new Uri(
            Factory.Server.BaseAddress,
            $"api/results?AnalyticalRunId={seededResults.AnalyticalRunId}&SortByProperty=ResultType&SortByAscending=false");

        var result = await Factory.HttpClient.GetFromJsonAsync<PartialQueryResult<AnalyticalRunTestResultDto>>(uri, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Items.Select(x => x.ResultType).Should().Equal(
            TestResultType.QualityControl,
            TestResultType.Sample);
    }

    [Fact]
    public async Task GetTestResultsShouldFindQualityControlByIdentity()
    {
        var seededResults = await SeedSampleAndQualityControlTests();
        var uri = new Uri(
            Factory.Server.BaseAddress,
            $"api/results?AnalyticalRunId={seededResults.AnalyticalRunId}&Identity={Uri.EscapeDataString(seededResults.QualityControlName)}");

        var result = await Factory.HttpClient.GetFromJsonAsync<PartialQueryResult<AnalyticalRunTestResultDto>>(uri, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Single().ResultType.Should().Be(TestResultType.QualityControl);
        result.Items.Single().SampleId.Should().Be(seededResults.QualityControlName);
    }

    [Fact]
    public async Task GetQualityControlDetailsShouldReportReplicateErrors()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var qualityControl = new QualityControlTestResult
        {
            InstrumentNumber = "SEED_001",
            MethodName = "sIgE",
            TestName = "c1",
            QualityControlName = "QC_ERROR",
            Replicates =
            [
                new QualityControlTestReplicate
                {
                    InstrumentReplicateId = Guid.NewGuid(),
                    ReplicateNumber = 1,
                    TestReplicateErrors =
                    [
                        new TestReplicateError
                        {
                            ErrorCode = 123
                        }
                    ]
                }
            ]
        };

        dbContext.QualityControlTestResults.Add(qualityControl);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uri = new Uri(Factory.Server.BaseAddress, $"api/results/qcdetails/{qualityControl.Id}");
        var result = await Factory.HttpClient.GetFromJsonAsync<QualityControlTestResultDto>(uri, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Replicates.Should().ContainSingle();
        result.Replicates.Single().HasError.Should().BeTrue();
    }

    private async Task<(int AnalyticalRunId, string SampleId, string QualityControlName)> SeedSampleAndQualityControlTests()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var paramDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var sampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength];
        var qualityControlName = _fixture.Create<string>()[..Constants.QualityControlNameLength];
        const string methodName = "sIgE";
        const string testName = "c1";
        const string instrumentNumber = "SEED_001";

        var concentrationUnit = new MethodConcentrationUnit
        {
            UnitId = 1,
            ConcentrationUnit = "kUA/l"
        };
        paramDbContext.MethodConcentrationUnits.Add(concentrationUnit);

        paramDbContext.Methods.Add(new Method
        {
            MethodName = methodName,
            FullMethodName = "ImmunoCAP Specific IgE 0-100",
            MethodNameLis = methodName,
            ConcentrationUnits = [concentrationUnit],
            Enabled = true,
            AnalyticalRun = new MethodAnalyticalRun(),
            Calculation = new MethodCalculation(),
            Presentation = _fixture.Create<MethodPresentation>(),
            Reporting = _fixture.Create<MethodReporting>(),
            Reagents =
            [
                new MethodReagent
                {
                    ReagentType = ReagentType.Conjugate,
                    DefaultReagentIdentity = methodName
                }
            ],
            Tests =
            [
                new MethodTest
                {
                    TestName = testName,
                    DefaultReportConcentration = true,
                    DefaultLabTestName = testName,
                    DiluentIdentity = string.Empty,
                    FullTestName = string.Empty,
                    DefaultConcentrationUnit = concentrationUnit
                }
            ]
        });

        await paramDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var analyticalRun = new AnalyticalRun
        {
            InstrumentAnalyticalRunId = default,
            MethodName = methodName,
            InstrumentNumber = instrumentNumber
        };
        dbContext.AnalyticalRuns.Add(analyticalRun);

        dbContext.SampleTestResults.Add(new SampleTestResult
        {
            AnalyticalRun = analyticalRun,
            InstrumentNumber = instrumentNumber,
            MethodName = methodName,
            TestName = testName,
            TestOrderId = Guid.NewGuid(),
            MeanConcentrationAsText = "50",
            MeanConcentration = 50,
            NumberOfReplicates = 1,
            MeanCutOff = "cutOff",
            MeanClass = "class",
            RequestUid =
            {
                RequestCreatedDate = DateTimeOffset.UtcNow,
                SampleId = sampleId
            },
            ApprovalStatus = ResultApprovalStatus.Approved,
            Status = TestResultStatus.Approved
        });

        dbContext.QualityControlTestResults.Add(new QualityControlTestResult
        {
            AnalyticalRun = analyticalRun,
            InstrumentNumber = instrumentNumber,
            MethodName = methodName,
            TestName = testName,
            QualityControlName = qualityControlName,
            MeanConcentrationAsText = "25",
            MeanConcentration = 25,
            NumberOfReplicates = 1,
            MeanCutOff = "cutOff",
            MeanClass = "class",
            Status = TestResultStatus.Ready
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (analyticalRun.Id, sampleId, qualityControlName);
    }
}
