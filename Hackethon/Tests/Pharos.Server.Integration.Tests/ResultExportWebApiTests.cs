using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Result;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Job;
using Pharos.Server.Result.Application.Queries.GetAnalyticalRuns;
using Pharos.Server.Result.Application.Queries.GetResultExport;
using Pharos.Server.Result.Domain;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Domain;
using Quartz;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class ResultExportWebApiTests : IntegrationTestBase
{
    private readonly Fixture _fixture;

    public ResultExportWebApiTests(IntegrationTestsWaf factory) : base(factory)
    {
        _fixture = new Fixture();
    }

    [Fact]
    public async Task GetAnalyticalRuns()
    {
        //Arrange
        var db = ServiceProvider.GetRequiredService<IResultDbContext>();
        var client = Factory.HttpClient;

        var analyticalRun = await CreateSomeAnalyticRunsAndUseLast(db);

        //Act
        var uri = new Uri(Factory.Server.BaseAddress, "api/results/analyticalRuns");
        var result = await client.GetFromJsonAsync<PartialQueryResult<AnalyticalRunDto>>(uri, TestContext.Current.CancellationToken);

        //Assert
        Assert.NotNull(result);
        var run = result.Items.SingleOrDefault(x => x.Id == analyticalRun.Id);
        Assert.NotNull(run);
        run.MethodName.Should().Be(analyticalRun.MethodName);
        run.StartDateTime.Should().Be(analyticalRun.StartDateTime);
    }

    [Fact]
    public async Task GetAnalyticalRunsExport()
    {
        //Arrange
        var db = ServiceProvider.GetRequiredService<IResultDbContext>();
        var client = Factory.HttpClient;

        var analyticalRun = await CreateSomeAnalyticRunsAndUseLast(db);
        var exportQuery = new GetResultExportQuery([analyticalRun.Id]);
        var idBytes = Encoding.UTF8.GetBytes(analyticalRun.InstrumentAnalyticalRunId.ToString());

        //Act
        using var createContent = new StringContent(JsonSerializer.Serialize(exportQuery), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(new Uri(Factory.Server.BaseAddress, "api/results/export"), createContent, TestContext.Current.CancellationToken);

        //Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Content.Headers.Should().ContainKey("Content-Disposition");

        var fileContent = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        fileContent.Should().NotBeEmpty().And.ContainInOrder(idBytes);
    }

    [Fact]
    public async Task AutoTestResultExport()
    {
        FileInfo? fileInfo = null;
        try
        {
            // Arrange

            DirectoryInfo di;
            int preCount;
            await using (var asyncScope = Factory.Services.CreateAsyncScope())
            {
                // override the export settings for testing
                var folder = "%PROGRAMDATA%/Thermo Fisher Scientific/Pharos/Results";
                var configuration = ServiceProvider.GetRequiredService<IConfiguration>();
                var sectionPath = SettingsServiceCollectionExtensions.GetSectionPath<ResultSettings>(configuration);
                var settingsDbContext = asyncScope.ServiceProvider.GetRequiredService<ISettingsDbContext>();
                settingsDbContext.Settings.Add(new Setting
                {
                    Key = $"{sectionPath}:{nameof(ResultSettings.Export)}:{nameof(ResultExportSettings.AnalyticalRunExportPath)}",
                    Value = folder
                });
                settingsDbContext.Settings.Add(new Setting
                {
                    Key = $"{sectionPath}:{nameof(ResultSettings.Export)}:{nameof(ResultExportSettings.ExportOnAnalyticalRunEnd)}",
                    Value = true.ToString()
                });
                await settingsDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

                di = new DirectoryInfo(Environment.ExpandEnvironmentVariables(folder));
                if (!di.Exists)
                {
                    Directory.CreateDirectory(di.FullName);
                }

                preCount = di.GetFiles("*", SearchOption.TopDirectoryOnly).Length;

                var db = asyncScope.ServiceProvider.GetRequiredService<IResultDbContext>();

                var analyticalRun = await CreateSomeAnalyticRunsAndUseLast(db);

                var methodName = analyticalRun.MethodName;
                var analyticalRunId = analyticalRun.InstrumentAnalyticalRunId.ToString();
                var runId = string.Empty;

                var mediator = asyncScope.ServiceProvider.GetRequiredService<IMediator>();

                // Act
                await mediator.Send(new EndAnalyticalRunCommand(analyticalRun.InstrumentNumber, methodName, analyticalRunId, runId), TestContext.Current.CancellationToken);
            }

            await WaitForNotificationsAsync();

            // Assert: a file is created

            // make sure the export job is completed before checking the file
            await using var scope = Factory.Services.CreateAsyncScope();
            var scheduler = await scope.ServiceProvider.GetRequiredService<ISchedulerFactory>().GetScheduler(TestContext.Current.CancellationToken);
            while (await scheduler.CheckExists(ExportAnalyticalRunJob.Key, TestContext.Current.CancellationToken))
            {
                await Task.Delay(50, TestContext.Current.CancellationToken);
            }

            var postCount = di.GetFiles("*", SearchOption.TopDirectoryOnly).Length;
            (postCount - preCount).Should().Be(1);
            fileInfo = await CheckSavedFile(di);
        }
        finally
        {
            fileInfo?.Delete();
        }
    }

    private static async Task<FileInfo> CheckSavedFile(DirectoryInfo di)
    {
        var fileInfo = di.GetFiles().OrderByDescending(f => f.LastWriteTime).First();
        var reader = fileInfo.OpenText();
        var text = await reader.ReadToEndAsync();
        reader.Close();
        text.Contains("[INFORMATION]").Should().BeTrue();
        return fileInfo;
    }

    private async Task<AnalyticalRun> CreateSomeAnalyticRunsAndUseLast(IResultDbContext db)
    {
        var instrumentNumber = _fixture.Create<string>()[..Constants.InstrumentSerialNumberLength];
        await CreateAnalyticalRunAndAddToDb(db, instrumentNumber);
        await CreateAnalyticalRunAndAddToDb(db, instrumentNumber);
        var ar = await CreateAnalyticalRunAndAddToDb(db, instrumentNumber);
        return ar;
    }

    private static async Task<AnalyticalRun> CreateAnalyticalRunAndAddToDb(IResultDbContext db, string instrumentNumber)
    {
        var method = "asfd";

        var curve = new Curve
        {
            InstrumentNumber = instrumentNumber,
            MethodName = method,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            CalculationType = CalculationType.Rodbard4Par,
            CurveFittedFlag = true,
            Judgment = CurveJudgment.OK,
            GroupId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid()
        };
        db.Curves.Add(curve);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ar = new AnalyticalRun
        {
            InstrumentNumber = instrumentNumber,
            StartDateTime = DateTimeOffset.Now,
            MethodName = method,
            CurveId = curve.Id,
            InstrumentAnalyticalRunId = Guid.NewGuid()
        };
        db.AnalyticalRuns.Add(ar);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return ar;
    }
}