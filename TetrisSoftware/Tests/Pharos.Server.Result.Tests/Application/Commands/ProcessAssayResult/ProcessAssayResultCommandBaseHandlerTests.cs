using MediatR;
using Microsoft.Extensions.Logging;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Result.Application.Commands.ProcessAssayResult;

[Trait("ProductRequirement", "PR_865")]
[Trait("ModuleRequirement", "MR_3525")]
public class ProcessAssayResultCommandBaseHandlerTests
{
    private const string _instrumentNumber = "SN001";
    private readonly Mock<IResultDbContext> _dbContextMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly List<AnalyticalRun> _analyticalRuns = [];
    private readonly ProcessSampleResultCommandHandler _sut;

    public ProcessAssayResultCommandBaseHandlerTests()
    {
        _dbContextMock.Setup(x => x.AnalyticalRuns).ReturnsDbSet(_analyticalRuns);
        _dbContextMock.Setup(x => x.TestReplicates).ReturnsDbSet(new List<TestReplicate>());
        _sut = new ProcessSampleResultCommandHandler(_dbContextMock.Object, Mock.Of<IMethodsService>(),
            Mock.Of<ILogger<ProcessSampleResultCommandHandler>>(), Mock.Of<IResultCalculationService>(),
            Mock.Of<IResponseCalculationService>(), Mock.Of<IResultFormatter>(), _mediatorMock.Object,
            Mock.Of<IReagentBarcodeService>());
    }

    [Theory]
    [InlineData(RunStatus.Ready, ResultApprovalStatus.NotApproved, false)]
    [InlineData(RunStatus.ReadyWithoutTests, ResultApprovalStatus.NotApproved, false)]
    [InlineData(RunStatus.ReadyWithoutTests, ResultApprovalStatus.Approved, false)]
    [InlineData(RunStatus.Ready, ResultApprovalStatus.Rejected, false)]
    [InlineData(RunStatus.Processing, ResultApprovalStatus.NotApproved, false)]
    [InlineData(RunStatus.Ready, ResultApprovalStatus.NotApproved, true)]
    public async Task EndedRun_DiscardsResultAndCreatesWarning(RunStatus runStatus, ResultApprovalStatus approvalStatus, bool allowRepeatedMessage)
    {
        var runId = Guid.NewGuid();
        _analyticalRuns.Add(new AnalyticalRun
        {
            InstrumentNumber = _instrumentNumber,
            InstrumentAnalyticalRunId = runId,
            EndDateTime = DateTimeOffset.Now,
            RunStatus = runStatus,
            ApprovalStatus = approvalStatus
        });
        var message = CreateMessage(runId.ToString());

        await _sut.Handle(new ProcessSampleResultCommand
        {
            InstrumentNumber = _instrumentNumber,
            Message = message,
            AllowRepeatedMessage = allowRepeatedMessage
        }, TestContext.Current.CancellationToken);

        VerifyResultDiscarded(message, runId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task EndedCatchAllRun_DiscardsResult(string analyticalRunId)
    {
        _analyticalRuns.Add(new AnalyticalRun
        {
            InstrumentNumber = _instrumentNumber,
            InstrumentAnalyticalRunId = Guid.Empty,
            EndDateTime = DateTimeOffset.Now
        });
        var message = CreateMessage(analyticalRunId);

        await _sut.Handle(new ProcessSampleResultCommand
        {
            InstrumentNumber = _instrumentNumber,
            Message = message
        }, TestContext.Current.CancellationToken);

        VerifyResultDiscarded(message, Guid.Empty);
    }

    private void VerifyResultDiscarded(AssayResult message, Guid runId)
    {
        _mediatorMock.Verify(x => x.Send(It.Is<CreatePharosErrorCommand>(command =>
            command.ErrorCode == PharosErrorCodes.ResultReceivedAfterRunEnd
            && command.Classification == ErrorClassification.Warning
            && command.InstrumentNumber == _instrumentNumber
            && command.AllowMultiple), TestContext.Current.CancellationToken), Times.Once);
        var warning = _mediatorMock.Invocations.Single().Arguments[0].Should().BeOfType<CreatePharosErrorCommand>().Subject;
        warning.Parameters.Should().Equal(message.MethodName, message.TestName, runId.ToString(), message.Replicate.ReplicateId);
        _dbContextMock.VerifyGet(x => x.SampleTestResults, Times.Never);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AssayResult CreateMessage(string analyticalRunId)
    {
        return new AssayResult
        {
            MethodName = "sIgE",
            TestName = "c1",
            TestType = EnumTestType.Sample,
            Run = new RunInfo { AnalyticalRunId = analyticalRunId },
            Replicate = new ReplicateInfo { ReplicateId = Guid.NewGuid().ToString() }
        };
    }
}