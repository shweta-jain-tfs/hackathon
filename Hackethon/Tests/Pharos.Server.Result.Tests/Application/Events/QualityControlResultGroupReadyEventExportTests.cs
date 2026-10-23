using System.Linq.Expressions;
using MediatR;
using Microsoft.Extensions.Options;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.LabCommunity;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Result.Application.Events.TestResultReady;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Mapping;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

public sealed class QualityControlResultGroupReadyEventExportTests
{
    private const string _instrumentNumber = "SN001";
    private const string _methodName = "Method1";
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly DateTimeOffset _measurementTime = new(2026, 9, 17, 9, 0, 0, TimeSpan.Zero);
    private readonly QualityControlResultGroupReadyEventHandler _sut;
    private SendQualityControlResultToLabCommunityCommand? _sentCommand;

    public QualityControlResultGroupReadyEventExportTests()
    {
        var replicate = new QualityControlTestReplicate
        {
            Id = 200,
            InstrumentReplicateId = Guid.NewGuid(),
            ReplicateNumber = 1,
            MeasurementDateTime = _measurementTime,
            Concentration = 12.8,
            Response = 100.2,
            TestReplicateReagents =
            [
                CreateReagent(200, ArticleType.ConjugateCartridge, "CON1"),
                CreateReagent(200, ArticleType.EliaWellCarrier, "SOL1"),
                CreateReagent(200, ArticleType.QualityControlCartridge, "QCL1")
            ]
        };
        var result = new QualityControlTestResult
        {
            GroupId = _groupId,
            InstrumentNumber = _instrumentNumber,
            MeasurementDateTime = _measurementTime,
            QualityControlName = "QC1",
            MethodName = _methodName,
            TestName = "Test1",
            QualityControlType = QualityControlType.Concentration,
            ExpectedConcentration = 12.5,
            ExpectedStdDev = 0.5,
            StdDevLimit = 2,
            MeanConcentration = 12.5,
            MeanConcentrationStatus = ConcentrationStatus.NotAvailable,
            Replicates = [replicate]
        };
        var dbContextMock = new Mock<IResultDbContext>();
        dbContextMock.Setup(db => db.QualityControlTestResults).ReturnsDbSet([result]);
        dbContextMock.Setup(db => db.TestReplicateErrors).ReturnsDbSet([]);

        var methodsServiceMock = new Mock<IMethodsService>();
        methodsServiceMock
            .Setup(service => service.GetQualityControl<double>(
                _methodName,
                "Test1",
                "QC1",
                It.IsAny<Expression<Func<PhadiaQualityControlTest, double>>>()))
            .ReturnsAsync(15d);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(mediator => mediator.Send(It.IsAny<SendQualityControlResultToLabCommunityCommand>(), It.IsAny<CancellationToken>()))
            .Callback<SendQualityControlResultToLabCommunityCommand, CancellationToken>((command, _) => _sentCommand = command);

        _sut = new QualityControlResultGroupReadyEventHandler(
            dbContextMock.Object,
            methodsServiceMock.Object,
            Mock.Of<IResultCalculationService>(),
            Mock.Of<IOptionsSnapshot<LisSettings>>(),
            mediatorMock.Object,
            new LisTestResultMapper(Mock.Of<IInstrumentManager>(), Mock.Of<IMethodsService>()));
    }

    [Trait("ProductRequirement", "PR_2184")]
    [Trait("ModuleRequirement", "MR_6059")]
    [Fact]
    public async Task SendsSendQualityControlResultToLabCommunity()
    {
        await _sut.Handle(
            new QualityControlResultGroupReadyEvent(_instrumentNumber, _groupId),
            TestContext.Current.CancellationToken);

        _sentCommand.Should().BeEquivalentTo(new SendQualityControlResultToLabCommunityCommand(
            _groupId,
            _instrumentNumber,
            _measurementTime,
            "QC1",
            _methodName,
            "Test1",
            QualityControlResultJudgment.Ok,
            "CON1",
            "SOL1",
            "QCL1",
            11.5,
            13.5,
            15,
            [new QualityControlResultReplicate(1, _measurementTime, 12.8, 100.2, QualityControlResultJudgment.Ok, "CON1", "SOL1", "QCL1")]));
    }

    private static TestReplicateReagent CreateReagent(int replicateId, ArticleType type, string lotNumber)
    {
        return new TestReplicateReagent
        {
            TestReplicateId = replicateId,
            ReagentType = type,
            LotNumber = lotNumber
        };
    }
}
