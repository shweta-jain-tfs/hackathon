using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using Pharos.Server.SystemErrors.Application.Interfaces;
using Pharos.Server.SystemErrors.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class ProcessAssayResultCommandTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    // ReSharper disable StringLiteralTypo
    private const string _devSolutionBarcode = "EK4XXXNZ0001";
    private const string _instrumentNumber = "SN001";
    private const string _otherInstrumentNumber = "SN002";
    private const string _assayMessageMethodNamesIgE = "sIgE";

    [Theory]
    [InlineData(EnumTestType.Sample)]
    [InlineData(EnumTestType.QualityControl)]
    [InlineData(EnumTestType.Calibrator)]
    [InlineData(EnumTestType.CurveControl)]
    [Trait("ProductRequirement", "PR_865")]
    [Trait("ModuleRequirement", "MR_3525")]
    public async Task ProcessAssayResult_EndedRun_DiscardsResult(EnumTestType testType)
    {
        var resultDbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var errorsDbContext = ServiceProvider.GetRequiredService<ISystemErrorsDbContext>();
        var message = CreateAssayMessage(Guid.NewGuid(), testType, EnumReagentType.UnspecifiedReagentType);
        var run = new AnalyticalRun
        {
            InstrumentNumber = _instrumentNumber,
            InstrumentAnalyticalRunId = Guid.Parse(message.Run.AnalyticalRunId),
            MethodName = message.MethodName,
            EndDateTime = DateTimeOffset.Now,
            RunStatus = RunStatus.ReadyWithoutTests
        };
        resultDbContext.AnalyticalRuns.Add(run);
        await resultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await SendCommandInScope(new ProcessAssayResultCommand(_instrumentNumber, message));

        resultDbContext.TestResults.Should().BeEmpty();
        resultDbContext.TestReplicates.Should().BeEmpty();
        var warning = await errorsDbContext.SystemErrors.AsNoTracking()
            .SingleAsync(x => x.Code == PharosErrorCodes.ResultReceivedAfterRunEnd, TestContext.Current.CancellationToken);
        warning.Source.Should().Be(ErrorSource.Pharos);
        warning.Classification.Should().Be(ErrorClassification.Warning);
        warning.SerialNumber.Should().Be(_instrumentNumber);
        warning.Parameters.Should().Equal(message.MethodName, message.TestName, message.Run.AnalyticalRunId, message.Replicate.ReplicateId);
        var storedRun = await resultDbContext.AnalyticalRuns.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        storedRun.EndDateTime.Should().Be(run.EndDateTime);
        storedRun.RunStatus.Should().Be(RunStatus.ReadyWithoutTests);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_865")]
    [Trait("ModuleRequirement", "MR_3525")]
    public async Task ProcessAssayResult_AfterRunEnd_DoesNotChangeExistingResult()
    {
        var resultDbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var errorsDbContext = ServiceProvider.GetRequiredService<ISystemErrorsDbContext>();
        var message = CreateAssayMessage(Guid.NewGuid(), EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);
        await SendCommandInScope(new ProcessAssayResultCommand(_instrumentNumber, message));
        await SendCommandInScope(new EndAnalyticalRunCommand(_instrumentNumber, message.MethodName, message.Run.AnalyticalRunId, message.Run.AssayRunId));

        var originalResult = await resultDbContext.TestResults.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        var lateMessage = message.Clone();
        lateMessage.Replicate.ReplicateId = Guid.NewGuid().ToString();
        lateMessage.FluorometerResult.MeasurementValue = 99999;

        await SendCommandInScope(new ProcessAssayResultCommand(_instrumentNumber, lateMessage));

        var storedResult = await resultDbContext.TestResults.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        storedResult.Id.Should().Be(originalResult.Id);
        storedResult.Status.Should().Be(originalResult.Status);
        var storedReplicate = await resultDbContext.TestReplicates.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        storedReplicate.InstrumentReplicateId.ToString().Should().Be(message.Replicate.ReplicateId);
        storedReplicate.FluorometerValue.Should().Be(message.FluorometerResult.MeasurementValue);
        var warning = await errorsDbContext.SystemErrors.AsNoTracking()
            .SingleAsync(x => x.Code == PharosErrorCodes.ResultReceivedAfterRunEnd, TestContext.Current.CancellationToken);
        warning.Parameters.Should().Equal(lateMessage.MethodName, lateMessage.TestName, lateMessage.Run.AnalyticalRunId, lateMessage.Replicate.ReplicateId);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_865")]
    [Trait("ModuleRequirement", "MR_3525")]
    public async Task ProcessAssayResult_RunClosedByNewRun_DiscardsLateResult()
    {
        var resultDbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var errorsDbContext = ServiceProvider.GetRequiredService<ISystemErrorsDbContext>();
        var message = CreateAssayMessage(Guid.NewGuid(), EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);
        await SendCommandInScope(new ProcessAssayResultCommand(_instrumentNumber, message));
        var newRunMessage = CreateAssayMessage(Guid.NewGuid(), EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);
        await SendCommandInScope(new ProcessAssayResultCommand(_instrumentNumber, newRunMessage));
        var runId = Guid.Parse(message.Run.AnalyticalRunId);
        var endedRun = await resultDbContext.AnalyticalRuns.AsNoTracking()
            .SingleAsync(x => x.InstrumentAnalyticalRunId == runId, TestContext.Current.CancellationToken);
        endedRun.EndDateTime.Should().NotBeNull();
        var lateMessage = message.Clone();
        lateMessage.Sample.SampleId = "LateSample";
        lateMessage.Replicate.GroupId = Guid.NewGuid().ToString();
        lateMessage.Replicate.ReplicateId = Guid.NewGuid().ToString();

        await SendCommandInScope(new ProcessAssayResultCommand(_instrumentNumber, lateMessage));

        resultDbContext.TestReplicates.Count().Should().Be(2);
        resultDbContext.TestResults.Count().Should().Be(2);
        var warning = await errorsDbContext.SystemErrors.AsNoTracking()
            .SingleAsync(x => x.Code == PharosErrorCodes.ResultReceivedAfterRunEnd, TestContext.Current.CancellationToken);
        warning.Parameters.Should().Equal(lateMessage.MethodName, lateMessage.TestName, lateMessage.Run.AnalyticalRunId, lateMessage.Replicate.ReplicateId);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_865")]
    [Trait("ModuleRequirement", "MR_3525")]
    public async Task ProcessAssayResult_FlrImport_EndedRun_DiscardsResult()
    {
        var resultDbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var errorsDbContext = ServiceProvider.GetRequiredService<ISystemErrorsDbContext>();
        var message = CreateAssayMessage(Guid.NewGuid(), EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);
        resultDbContext.AnalyticalRuns.Add(new AnalyticalRun
        {
            InstrumentNumber = _instrumentNumber,
            InstrumentAnalyticalRunId = Guid.Parse(message.Run.AnalyticalRunId),
            MethodName = message.MethodName,
            EndDateTime = DateTimeOffset.Now,
            RunStatus = RunStatus.ReadyWithoutTests
        });
        await resultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var command = new ProcessAssayResultCommand(_instrumentNumber, message, AllowRepeatedMessage: true);

        await SendCommandInScope(command);

        resultDbContext.TestResults.Should().BeEmpty();
        resultDbContext.TestReplicates.Should().BeEmpty();
        var warning = await errorsDbContext.SystemErrors.AsNoTracking()
            .SingleAsync(x => x.Code == PharosErrorCodes.ResultReceivedAfterRunEnd, TestContext.Current.CancellationToken);
        warning.Classification.Should().Be(ErrorClassification.Warning);
        warning.SerialNumber.Should().Be(_instrumentNumber);
        warning.Parameters.Should().Equal(message.MethodName, message.TestName, message.Run.AnalyticalRunId, message.Replicate.ReplicateId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("ProductRequirement", "PR_865")]
    [Trait("ModuleRequirement", "MR_3525")]
    public async Task ProcessAssayResult_FlrImport_SkipsDuplicate(bool endRun)
    {
        var resultDbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var errorsDbContext = ServiceProvider.GetRequiredService<ISystemErrorsDbContext>();
        var message = CreateAssayMessage(Guid.NewGuid(), EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);
        var command = new ProcessAssayResultCommand(_instrumentNumber, message, AllowRepeatedMessage: true);

        await SendCommandInScope(command);
        if (endRun)
        {
            await SendCommandInScope(new EndAnalyticalRunCommand(_instrumentNumber, message.MethodName, message.Run.AnalyticalRunId, message.Run.AssayRunId));
        }
        await SendCommandInScope(command);

        resultDbContext.TestResults.Should().ContainSingle();
        resultDbContext.TestReplicates.Should().ContainSingle();
        errorsDbContext.SystemErrors.Any(x => x.Code == PharosErrorCodes.ResultReceivedAfterRunEnd).Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("ProductRequirement", "PR_865")]
    [Trait("ModuleRequirement", "MR_3525")]
    public async Task ProcessAssayResult_OpenOrUnknownRun_StoresResult(bool runExists)
    {
        var resultDbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var errorsDbContext = ServiceProvider.GetRequiredService<ISystemErrorsDbContext>();
        var message = CreateAssayMessage(Guid.NewGuid(), EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);
        if (runExists)
        {
            resultDbContext.AnalyticalRuns.Add(new AnalyticalRun
            {
                InstrumentNumber = _instrumentNumber,
                InstrumentAnalyticalRunId = Guid.Parse(message.Run.AnalyticalRunId),
                MethodName = message.MethodName,
                RunStatus = RunStatus.Processing
            });
            await resultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await SendCommandInScope(new ProcessAssayResultCommand(_instrumentNumber, message));

        resultDbContext.TestResults.Should().ContainSingle();
        var replicate = await resultDbContext.TestReplicates.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        replicate.InstrumentReplicateId.Should().Be(Guid.Parse(message.Replicate.ReplicateId));
        var run = await resultDbContext.AnalyticalRuns.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        run.InstrumentAnalyticalRunId.Should().Be(Guid.Parse(message.Run.AnalyticalRunId));
        run.EndDateTime.Should().BeNull();
        errorsDbContext.SystemErrors.Any(x => x.Code == PharosErrorCodes.ResultReceivedAfterRunEnd).Should().BeFalse();
    }

    [Theory]
    [InlineData(_otherInstrumentNumber, true)]
    [InlineData(_instrumentNumber, false)]
    [Trait("ProductRequirement", "PR_865")]
    [Trait("ModuleRequirement", "MR_3525")]
    public async Task ProcessAssayResult_UnrelatedEndedRun_StoresResult(string endedRunInstrument, bool sameRunId)
    {
        var resultDbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var errorsDbContext = ServiceProvider.GetRequiredService<ISystemErrorsDbContext>();
        var message = CreateAssayMessage(Guid.NewGuid(), EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);
        resultDbContext.AnalyticalRuns.Add(new AnalyticalRun
        {
            InstrumentNumber = endedRunInstrument,
            InstrumentAnalyticalRunId = sameRunId ? Guid.Parse(message.Run.AnalyticalRunId) : Guid.NewGuid(),
            MethodName = message.MethodName,
            EndDateTime = DateTimeOffset.Now,
            RunStatus = RunStatus.ReadyWithoutTests
        });
        await resultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await SendCommandInScope(new ProcessAssayResultCommand(_instrumentNumber, message));

        var result = await resultDbContext.TestResults.AsNoTracking().Include(x => x.AnalyticalRun)
            .SingleAsync(TestContext.Current.CancellationToken);
        result.AnalyticalRun!.InstrumentNumber.Should().Be(_instrumentNumber);
        result.AnalyticalRun.InstrumentAnalyticalRunId.Should().Be(Guid.Parse(message.Run.AnalyticalRunId));
        result.AnalyticalRun.EndDateTime.Should().BeNull();
        resultDbContext.TestReplicates.Should().ContainSingle();
        errorsDbContext.SystemErrors.Any(x => x.Code == PharosErrorCodes.ResultReceivedAfterRunEnd).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [Trait("ProductRequirement", "PR_865")]
    [Trait("ModuleRequirement", "MR_3525")]
    public async Task ProcessAssayResult_EmptyRunId_StoresResultInCatchAllRun(string analyticalRunId)
    {
        var resultDbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var errorsDbContext = ServiceProvider.GetRequiredService<ISystemErrorsDbContext>();
        var message = CreateAssayMessage(Guid.NewGuid(), EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);
        message.Run.AnalyticalRunId = analyticalRunId;

        await SendCommandInScope(new ProcessAssayResultCommand(_instrumentNumber, message));

        resultDbContext.TestResults.Should().ContainSingle();
        resultDbContext.TestReplicates.Should().ContainSingle();
        var run = await resultDbContext.AnalyticalRuns.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        run.InstrumentAnalyticalRunId.Should().Be(Guid.Empty);
        errorsDbContext.SystemErrors.Any(x => x.Code == PharosErrorCodes.ResultReceivedAfterRunEnd).Should().BeFalse();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_865")]
    [Trait("ModuleRequirement", "MR_3525")]
    public async Task ProcessAssayResult_InvalidRunId_IsValidatedBeforeRunStateCheck()
    {
        var requestDbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var resultDbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var message = CreateAssayMessage(Guid.NewGuid(), EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);
        message.Run.AnalyticalRunId = "not-a-guid";

        var action = () => SendCommandInScope(new ProcessAssayResultCommand(_instrumentNumber, message));

        await action.Should().ThrowAsync<FluentValidation.ValidationException>();
        requestDbContext.Requests.Should().BeEmpty();
        requestDbContext.TestOrders.Should().BeEmpty();
        resultDbContext.TestResults.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldCreateTestResult()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        dbContext.TestResults.Count().Should().Be(0);
        dbContext.TestReplicates.Count().Should().Be(0);

        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(Guid.NewGuid(), EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        dbContext.TestResults.Count().Should().Be(1);
        dbContext.TestReplicates.Count().Should().Be(1);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3499")]
    public async Task WithoutBlankResponseIsNotCalculated()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();

        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        //Assert RU have been calculated and saved. 
        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();
        replicate!.Response.Should().BeNull();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3499")]
    public async Task CalculationOfRU_SaveToDataBase_Sample()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        //Assert RU have been calculated and saved. 
        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();
        replicate!.Response.Should().NotBeNull();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3499")]
    public async Task CalculationOfRU_SaveToDataBase_Calibrator()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var paramDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var instrumentReplicateId = Guid.NewGuid();

        await SeedArticle(paramDbContext, ArticleType.CalibratorCartridge, "A5S");

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.Calibrator, EnumReagentType.CalibratorCartridge, "A5SXXXSS0001"));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        //Assert RU have been calculated and saved with the replicate. 
        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();
        replicate!.Response.Should().NotBeNull();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3499")]
    public async Task CalculationOfRU_SaveToDataBase_CurveControl()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var paramDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var instrumentReplicateId = Guid.NewGuid();

        await SeedArticle(paramDbContext, ArticleType.CurveControlCartridge, "A0V");

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.CurveControl, EnumReagentType.CurveControlCartridge, "A0VXXXSS0001"));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        //Assert RU have been calculated and saved with the replicate. 
        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();
        replicate!.Response.Should().NotBeNull();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3499")]
    public async Task CalculationOfRU_SaveToDataBase_QualityControl()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var paramDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var instrumentReplicateId = Guid.NewGuid();

        await SeedArticle(paramDbContext, ArticleType.QualityControlCartridge, "AVM");

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.QualityControl, EnumReagentType.QualityControlCartridge, "AVMXXXSS0001"));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        //Assert RU have been calculated and saved with the replicate. 
        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();
        replicate!.Response.Should().NotBeNull();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3501")]
    public async Task UseLatestMatchingReagentBlank()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode, isOk: false);
        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        //Assert RU have been calculated and saved. 
        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();

        //Assert that the Id of the blank that fulfills the criteria is used. 
        replicate!.UsedBlankId.Should().Be(3);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3525")]
    public async Task CalculationOfResults_AssayResult_StoreFluorometerValues()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();
        var fluorometerValue = replicate!.FluorometerValue;
        fluorometerValue.Should().NotBeNull();
        fluorometerValue.Should().Be(15000);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3527")]
    public async Task CalculationOfResults_AssayResult_StoreUsedReagents_Sample()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();
        var testReplicateReagents = replicate!.TestReplicateReagents;
        testReplicateReagents.Should().NotBeNullOrEmpty();

        if (testReplicateReagents is { Count: > 0 })
        {
            CheckReagents(testReplicateReagents, ArticleType.None, string.Empty);
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task CalculationOfResults_AssayResult_StoreRepeatedDiluentOnce(int dilutionSteps)
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var parametersDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();
        await SeedArticle(parametersDbContext, ArticleType.DiluentCartridge, "EH4");
        var message = CreateAssayMessage(instrumentReplicateId, EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);

        // The FLR for bug 40687 reuses this diluent cartridge cell across dilution steps.
        for (var step = 0; step < dilutionSteps; step++)
        {
            message.Reagents.Add(new ReagentInformation
            {
                ReagentType = EnumReagentType.DiluentCartridge,
                ReagentBarcode = "|EH4GBJL10001",
                CellPosition = 1,
                ExpiredReagent = false
            });
        }

        await mediator.Send(new ProcessAssayResultCommand(_instrumentNumber, message), TestContext.Current.CancellationToken);

        var replicate = await dbContext.TestReplicates.AsNoTracking().Include(r => r.TestReplicateReagents)
            .SingleAsync(r => r.InstrumentReplicateId == instrumentReplicateId, TestContext.Current.CancellationToken);
        var reagents = replicate.TestReplicateReagents!;
        reagents.Should().HaveCount(message.Reagents.Count - dilutionSteps + 1);
        var diluent = reagents.Should().ContainSingle(r => r.ReagentType == ArticleType.DiluentCartridge).Subject;
        diluent.Barcode.Should().Be("|EH4GBJL10001");
        diluent.CellPosition.Should().Be(1);
        diluent.Expired.Should().BeFalse();
    }

    [Theory]
    [InlineData("|EH4GBJL10001", 2, false, EnumReagentType.DiluentCartridge)]
    [InlineData("|EH4GBJL10001", 1, true, EnumReagentType.DiluentCartridge)]
    [InlineData("|EH4GBJL10002", 1, false, EnumReagentType.DiluentCartridge)]
    [InlineData("|EH4GBJL10001", 1, false, EnumReagentType.DevelopmentSolutionCartridge)]
    public async Task CalculationOfResults_AssayResult_PreserveDistinctReagents(string barcode, int cellPosition, bool expired, EnumReagentType reagentType)
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var parametersDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();
        await SeedArticle(parametersDbContext, ArticleType.DiluentCartridge, "EH4");
        var message = CreateAssayMessage(instrumentReplicateId, EnumTestType.Sample, EnumReagentType.DiluentCartridge, "|EH4GBJL10001");
        var distinctReagent = message.Reagents.Last().Clone();
        distinctReagent.ReagentBarcode = barcode;
        distinctReagent.CellPosition = cellPosition;
        distinctReagent.ExpiredReagent = expired;
        distinctReagent.ReagentType = reagentType;
        message.Reagents.Add(distinctReagent);

        await mediator.Send(new ProcessAssayResultCommand(_instrumentNumber, message), TestContext.Current.CancellationToken);

        var replicate = await dbContext.TestReplicates.AsNoTracking().Include(r => r.TestReplicateReagents)
            .SingleAsync(r => r.InstrumentReplicateId == instrumentReplicateId, TestContext.Current.CancellationToken);

        replicate.TestReplicateReagents.Should().HaveCount(message.Reagents.Count);
        replicate.TestReplicateReagents.Should().ContainSingle(r => r.ReagentType == ArticleType.DiluentCartridge
            && r.Barcode == "|EH4GBJL10001" && r.CellPosition == 1 && !r.Expired);
        replicate.TestReplicateReagents.Should().ContainSingle(r => r.Barcode == barcode && r.CellPosition == cellPosition
            && r.Expired == expired && r.ReagentType == (reagentType == EnumReagentType.DiluentCartridge
                ? ArticleType.DiluentCartridge : ArticleType.DevelopmentSolutionCartridge));
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3527")]
    public async Task CalculationOfResults_AssayResult_StoreUsedReagents_Calibrator()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var paramDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SeedArticle(paramDbContext, ArticleType.CalibratorCartridge, "A5S");

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.Calibrator, EnumReagentType.CalibratorCartridge, "A5SXXXSS0001"));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();
        var testReplicateReagents = replicate!.TestReplicateReagents;
        testReplicateReagents.Should().NotBeNullOrEmpty();

        if (testReplicateReagents is { Count: > 0 })
        {
            CheckReagents(testReplicateReagents, ArticleType.CalibratorCartridge, "A5SXXXSS0001");
        }
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3527")]
    public async Task CalculationOfResults_AssayResult_StoreUsedReagents_CurveControl()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var paramDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SeedArticle(paramDbContext, ArticleType.CurveControlCartridge, "A0V");

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.CurveControl, EnumReagentType.CurveControlCartridge, "A0VXXXSS0001"));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();
        var testReplicateReagents = replicate!.TestReplicateReagents;
        testReplicateReagents.Should().NotBeNullOrEmpty();

        if (testReplicateReagents is { Count: > 0 })
        {
            CheckReagents(testReplicateReagents, ArticleType.CurveControlCartridge, "A0VXXXSS0001");
        }
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3527")]
    public async Task CalculationOfResults_AssayResult_StoreUsedReagents_QualityControl()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var paramDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SeedArticle(paramDbContext, ArticleType.QualityControlCartridge, "AVM");

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.QualityControl, EnumReagentType.QualityControlCartridge, "AVMXXXSS0001"));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();
        var testReplicateReagents = replicate!.TestReplicateReagents;
        testReplicateReagents.Should().NotBeNullOrEmpty();

        if (testReplicateReagents is { Count: > 0 })
        {
            CheckReagents(testReplicateReagents, ArticleType.QualityControlCartridge, "AVMXXXSS0001");
        }
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3527")]
    public async Task CalculationOfResults_AssayResult_StoreProcessingErrors()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, CreateAssayMessage(instrumentReplicateId, EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType));
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();
        var testReplicateErrors = replicate!.TestReplicateErrors;
        testReplicateErrors.Should().NotBeNullOrEmpty();

        if (testReplicateErrors is { Count: > 0 })
        {
            testReplicateErrors.Count.Should().Be(4);
            testReplicateErrors.Any(e => e.ErrorCode == (int)EnumErrorCode.UnspecifiedErrorCode).Should().BeTrue();
            testReplicateErrors.Any(e => e.ErrorCode == 1).Should().BeTrue();
            testReplicateErrors.Any(e => e.ErrorCode == 2).Should().BeTrue();
            testReplicateErrors.Any(e => e.ErrorCode == 3).Should().BeTrue();
        }
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3529")]
    public async Task CalculationOfResults_AssayResult_ConnectToAnalyticalRun()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var assayResultMessage = CreateAssayMessage(instrumentReplicateId, EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, assayResultMessage);
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        var testResult = await dbContext.TestResults.Where(x => x.Replicates!.Any(r => r.InstrumentReplicateId == instrumentReplicateId))
            .Include(x => x.Replicates)
            .SingleOrDefaultAsync(TestContext.Current.CancellationToken);

        testResult.Should().NotBeNull();
        testResult.Replicates.Should().NotBeNullOrEmpty();
        testResult.Replicates!.Count.Should().Be(1);

        var analyticalRun = await dbContext.AnalyticalRuns.FirstOrDefaultAsync(a => a.Id == testResult.AnalyticalRunId, TestContext.Current.CancellationToken);
        analyticalRun.Should().NotBeNull();
        analyticalRun!.InstrumentAnalyticalRunId.ToString().Should().Be(assayResultMessage.Run.AnalyticalRunId);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3530")]
    public async Task CalculationOfResults_AssayResult_NewAnalyticalRun()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();

        var nRuns = await CreateProcessingAnalyticalRuns(dbContext);

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var assayResultMessage = CreateAssayMessage(instrumentReplicateId, EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, assayResultMessage);
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .SingleOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();

        await CheckProcessingAnalyticalRuns(dbContext, assayResultMessage.Run.AnalyticalRunId, nRuns + 1);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3530")]
    public async Task CalculationOfResults_AssayResult_NewAnalyticalRun_PreviousRunWithSampleTests_SetReady()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();

        var nRuns = await CreateProcessingAnalyticalRuns(dbContext);

        var existingProcessingRun = await dbContext.AnalyticalRuns
            .SingleAsync(a => a.InstrumentNumber == _instrumentNumber && a.MethodName == _assayMessageMethodNamesIgE && a.RunStatus == RunStatus.Processing, TestContext.Current.CancellationToken);

        await AddSampleTestResultToAnalyticalRun(dbContext, existingProcessingRun.Id);

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var assayResultMessage = CreateAssayMessage(instrumentReplicateId, EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, assayResultMessage);
        await mediator.Send(notification, TestContext.Current.CancellationToken);

        var replicate = await dbContext.TestReplicates.Where(x => x.InstrumentReplicateId == instrumentReplicateId)
            .SingleOrDefaultAsync(TestContext.Current.CancellationToken);

        replicate.Should().NotBeNull();

        await CheckProcessingAnalyticalRuns(dbContext, assayResultMessage.Run.AnalyticalRunId, nRuns + 1, RunStatus.Ready);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3531")]
    public async Task CalculationOfResults_AssayResult_AnalyticalRunsCurve()
    {
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var instrumentReplicateId = Guid.NewGuid();

        await CreateCurrentCurves(dbContext);

        // Assert no replicates before processing result
        dbContext.TestReplicates.Count().Should().Be(0);

        await SendBlankResult(mediator, _instrumentNumber, _devSolutionBarcode);
        var assayResultMessage = CreateAssayMessage(instrumentReplicateId, EnumTestType.Sample, EnumReagentType.UnspecifiedReagentType);
        var notification = new ProcessAssayResultCommand(_instrumentNumber, assayResultMessage);
        await mediator.Send(notification, TestContext.Current.CancellationToken);


        var testResult = await dbContext.TestResults.Where(x => x.Replicates!.Any(r => r.InstrumentReplicateId == instrumentReplicateId))
            .Include(x => x.AnalyticalRun)
            .Include(x => x.Replicates!)
            .ThenInclude(r => r.TestReplicateReagents)
            .SingleOrDefaultAsync(TestContext.Current.CancellationToken);

        testResult.Should().NotBeNull();
        testResult.Replicates.Should().NotBeNullOrEmpty();
        testResult.Replicates!.Count.Should().Be(1);

        var conjugateLotNumber = testResult.Replicates[0].TestReplicateReagents!
            .Where(r => r.ReagentType == ArticleType.ConjugateCartridge)
            .Select(r => r.LotNumber).SingleOrDefault();
        var calculationCurveId = testResult.AnalyticalRun!.CurveId;

        await CheckAssignedCurve(dbContext, conjugateLotNumber, _assayMessageMethodNamesIgE, calculationCurveId);
    }

    private async Task SendCommandInScope(IRequest command)
    {
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(command, TestContext.Current.CancellationToken);
        }
        await WaitForNotificationsAsync();
    }

    private static async Task CreateCurrentCurves(IResultDbContext dbContext)
    {
        // Wrong conjugate lot number
        await CreateCurrentCurve(dbContext, _instrumentNumber, _assayMessageMethodNamesIgE, "BGR02");

        // Wrong method
        await CreateCurrentCurve(dbContext, _instrumentNumber, "tIgE", "BGR01");

        // Wrong instrument
        await CreateCurrentCurve(dbContext, _otherInstrumentNumber, _assayMessageMethodNamesIgE, "BGR01");

        // Matching current curve
        await CreateCurrentCurve(dbContext, _instrumentNumber, _assayMessageMethodNamesIgE, "BGR01");

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task CreateCurrentCurve(IResultDbContext dbContext, string instrumentNumber, string methodName, string conjugateLotNumber)
    {
        // Insert another curve first, to let ID range be different between Curves and CurrentCurves...
        var anotherCurve = new Curve
        {
            ConjugateLotNumber = conjugateLotNumber,
            MethodName = methodName,
            InstrumentNumber = instrumentNumber,
            GroupId = Guid.NewGuid(),
            MeasurementDateTime = DateTimeOffset.Now,
            InstrumentRunId = Guid.NewGuid()
        };
        await dbContext.Curves.AddAsync(anotherCurve);

        var currentCurve = new CurrentCurve
        {
            ConjugateLotNumber = conjugateLotNumber,
            MethodName = methodName,
            Curve = new Curve
            {
                ConjugateLotNumber = conjugateLotNumber,
                MethodName = methodName,
                InstrumentNumber = instrumentNumber,
                GroupId = Guid.NewGuid(),
                MeasurementDateTime = DateTimeOffset.Now,
                Judgment = CurveJudgment.OK,
                InstrumentRunId = Guid.NewGuid()
            }
        };
        await dbContext.CurrentCurves.AddAsync(currentCurve);
    }

    private static async Task CheckAssignedCurve(IResultDbContext dbContext, string? conjugateLotNumber, string methodName, int? calculationCurveId)
    {
        conjugateLotNumber.Should().NotBeNull();
        calculationCurveId.Should().NotBeNull();

        var expectedCurve = await dbContext.CurrentCurves
            .Where(c => c.ConjugateLotNumber == conjugateLotNumber && c.MethodName == methodName && c.Curve.InstrumentNumber == _instrumentNumber)
            .Select(c => c.Curve)
            .SingleOrDefaultAsync(TestContext.Current.CancellationToken);

        expectedCurve.Should().NotBeNull();
        expectedCurve!.Id.Should().Be(calculationCurveId);
    }

    private static async Task<int> CreateProcessingAnalyticalRuns(IResultDbContext dbContext)
    {
        var nRuns = 0;

        // Processing: same instrument and method
        nRuns = await CreateProcessingAnalyticalRun(dbContext, _instrumentNumber, _assayMessageMethodNamesIgE, nRuns);

        // Processing: same instrument and different method
        nRuns = await CreateProcessingAnalyticalRun(dbContext, _instrumentNumber, "tIgE", nRuns);

        // Processing: different instrument and same method
        nRuns = await CreateProcessingAnalyticalRun(dbContext, _otherInstrumentNumber, _assayMessageMethodNamesIgE, nRuns);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return nRuns;
    }

    private static async Task<int> CreateProcessingAnalyticalRun(IResultDbContext dbContext, string instrumentNumber, string methodName, int nRuns)
    {
        var analyticalRun = new AnalyticalRun()
        {
            MethodName = methodName,
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            InstrumentNumber = instrumentNumber,
            RunStatus = RunStatus.Processing
        };
        await dbContext.AnalyticalRuns.AddAsync(analyticalRun);

        return ++nRuns;
    }

    private static async Task AddSampleTestResultToAnalyticalRun(IResultDbContext dbContext, int analyticalRunId)
    {
        var sampleTestResult = new SampleTestResult
        {
            AnalyticalRunId = analyticalRunId,
            Status = TestResultStatus.PendingApproval,
            MeasurementDateTime = DateTimeOffset.Now,
            MethodName = _assayMessageMethodNamesIgE,
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = _instrumentNumber
        };

        await dbContext.SampleTestResults.AddAsync(sampleTestResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task CheckProcessingAnalyticalRuns(IResultDbContext dbContext, string analyticalRunId, int nRuns, RunStatus expectedStatusForPreviousSameMethodRun = RunStatus.ReadyWithoutTests)
    {
        var analyticalRuns = await dbContext.AnalyticalRuns.ToListAsync(TestContext.Current.CancellationToken);
        analyticalRuns.Should().NotBeNullOrEmpty();
        analyticalRuns.Count.Should().Be(nRuns);

        var processingAnalyticalRun = analyticalRuns.SingleOrDefault(a => a.InstrumentAnalyticalRunId.ToString() == analyticalRunId);
        processingAnalyticalRun.Should().NotBeNull();
        processingAnalyticalRun!.MethodName.Should().Be(_assayMessageMethodNamesIgE);
        processingAnalyticalRun.InstrumentNumber.Should().Be(_instrumentNumber);
        processingAnalyticalRun.RunStatus.Should().Be(RunStatus.Processing);

        foreach (var ar in analyticalRuns.Where(a => a.InstrumentAnalyticalRunId.ToString() != analyticalRunId))
        {
            if (ar.InstrumentNumber == _instrumentNumber)
            {
                ar.RunStatus.Should().Be(ar.MethodName == _assayMessageMethodNamesIgE ? expectedStatusForPreviousSameMethodRun : RunStatus.Processing);
            }
            else
            {
                ar.RunStatus.Should().Be(RunStatus.Processing);
            }
        }
    }

    private static void CheckReagents(IList<TestReplicateReagent> testReplicateReagents, ArticleType articleType, string? reagentBarcode = null, string? reagentUdi = null)
    {
        CheckReagent(testReplicateReagents, ArticleType.ImmunoCapCarrier, "409XX01");
        CheckReagent(testReplicateReagents, ArticleType.ConjugateCartridge, "BGRXXX010001");
        CheckReagent(testReplicateReagents, ArticleType.DevelopmentSolutionCartridge, _devSolutionBarcode);
        CheckReagent(testReplicateReagents, ArticleType.StopSolutionBottle, null, "|01510222222333361715070710A213B1|991298");
        if (articleType != ArticleType.None)
        {
            CheckReagent(testReplicateReagents, articleType, reagentBarcode, reagentUdi);
        }
    }

    private static void CheckReagent(IList<TestReplicateReagent> testReplicateReagents, ArticleType articleType, string? reagentBarcode = null, string? reagentUdi = null)
    {
        var reagent = testReplicateReagents.FirstOrDefault(r => r.ReagentType == articleType);
        reagent.Should().NotBeNull();
        if (reagent.ReagentType is ArticleType.StopSolutionBottle or ArticleType.WashAdditiveBottle or ArticleType.WashConcentrateBottle)
        {
            _ = reagent.Udi.Should().Be(reagentUdi);
        }
        else
        {
            reagent.Barcode.Should().Be(reagentBarcode);
        }
    }

    private static Task SendBlankResult(IMediator mediator, string instrumentNumber, string devSolutionBarcode, int cellPosition = 1, bool isOk = true)
    {
        var blankResult = new BlankResult
        {
            BlankType = EnumBlankType.ReagentImmunoCap,
            ResultOk = isOk,
            MeanValue = 10,
            UpperLimit = 9999,
            LowerLimit = 0,
            DevelopmentSolutionIdentity = new DevelopmentSolutionIdentity
            {
                ReagentBarcode = devSolutionBarcode,
                CellPosition = cellPosition,
            },
            StopSolutionBarcode = "DoesNotMatter"
        };

        var notification = new ProcessBlankResultCommand(instrumentNumber, blankResult);
        return mediator.Send(notification, TestContext.Current.CancellationToken);
    }

    private static async Task SeedArticle(IParametersDbContext dbContext, ArticleType articleType, string articleNumber)
    {
        var article = new Article
        {
            ArticleNumber = articleNumber,
            ArticleType = articleType,
            Quantity = 1,
            MaxDaysInInstrument = 99,
        };
        dbContext.Articles.Add(article);

        article = new Article
        {
            ArticleNumber = "409",
            ArticleType = ArticleType.ImmunoCapCarrier,
            FullName = "Penicilloyl G",
            Identity = "c1",
            Quantity = 1,
            MaxDaysInInstrument = 99,
        };

        dbContext.Articles.Add(article);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static AssayResult CreateAssayMessage(Guid instrumentReplicateId, EnumTestType testType, EnumReagentType reagentType, string cartridgeBarcode = "")
    {
        var assay = new AssayResult
        {
            TestType = testType,
            TimeOfMeasurement = Timestamp.FromDateTimeOffset(DateTimeOffset.Parse("2023-09-29 11:17:00", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)),
            MethodName = _assayMessageMethodNamesIgE,
            TestName = "c1",
            InstrumentDilutionFactor = 1,
            Sample = new SampleInformation()
            {
                SampleId = "SampleId",
                SampleDate = Timestamp.FromDateTimeOffset(DateTimeOffset.Parse("2023-09-29 11:17:00", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)),
                PreDilutionFactor = 1
            },
            Replicate = new ReplicateInfo
            {
                ReplicateId = instrumentReplicateId.ToString(),
                GroupId = Guid.NewGuid().ToString(),
                ReplicatesInTestOrder = 2,
            },
            Run = new RunInfo
            {
                AnalyticalRunId = Guid.NewGuid().ToString(),
                AssayRunId = Guid.NewGuid().ToString(),
            },
            FluorometerResult = new FluorometerValue
            {
                MeasurementValue = 15000
            },
            Reagents =
            {
                new ReagentInformation
                {
                    ReagentType = EnumReagentType.ImmunoCapCarrier,
                    ReagentBarcode = "409XX01",
                    CellPosition = 1,
                    ExpiredReagent = false
                },
                new ReagentInformation
                {
                    ReagentType = EnumReagentType.DevelopmentSolutionCartridge,
                    ReagentBarcode = _devSolutionBarcode,
                    ReagentUdiBarcode = "|01510222222333361715070710A213B1|991244",
                    CellPosition = 1,
                    ExpiredReagent = false
                },
                new ReagentInformation
                {
                    ReagentType = EnumReagentType.ConjugateCartridge,
                    ReagentBarcode = "BGRXXX010001",
                    ReagentUdiBarcode = "|01510222222333361715070710A213B1|991299",
                    CellPosition = 1,
                    ExpiredReagent = false
                },
                new ReagentInformation
                {
                    ReagentType = EnumReagentType.StopSolution,
                    ReagentBarcode = "ES1XXX010001",
                    ReagentUdiBarcode = "|01510222222333361715070710A213B1|991298",
                    CellPosition = 1,
                    ExpiredReagent = false
                },
            },
            Errors =
            {
                EnumErrorCode.UnspecifiedErrorCode,
                (EnumErrorCode)1, (EnumErrorCode) 2, (EnumErrorCode) 3
            }
        };

        if (reagentType != EnumReagentType.UnspecifiedReagentType)
        {
            assay.Reagents.Add(new ReagentInformation
            {
                ReagentBarcode = cartridgeBarcode,
                ReagentUdiBarcode = "|01510222222333361715070710A213B1|991234",
                ReagentType = reagentType,
                CellPosition = 1,
                ExpiredReagent = false
            });
        }
        return assay;
    }
}
