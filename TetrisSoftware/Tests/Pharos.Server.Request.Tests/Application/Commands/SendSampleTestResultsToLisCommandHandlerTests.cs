using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Request.Application.Commands;

public class SendSampleTestResultsToLisCommandHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<IOptionsSnapshot<TubeTypesSettings>> _tubeSettingsMock = new();
    private readonly Mock<IOptionsSnapshot<LisSettings>> _lisSettingsMock = new();

    [Fact]
    public async Task Handle_WithPendingResult_ShouldPopulateReportSettingsFromMethodTest()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var request = new Domain.Request
        {
            Id = 1,
            SampleId = "SAMPLE-1",
            Source = RequestSource.Lis
        };
        var testOrder = new TestOrder
        {
            Id = 10,
            Uid = testOrderUid,
            MethodName = "sIgE",
            TestName = "t3",
            LabTestName = "LT-t3",
            State = TestOrderState.NotStarted,
            Request = request
        };
        request.TestOrders.Add(testOrder);

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet(new[] { testOrder });
        _tubeSettingsMock.SetupGet(x => x.Value).Returns(new TubeTypesSettings());
        _lisSettingsMock.SetupGet(x => x.Value).Returns(new LisSettings());
        var methodsService = new TestMethodsService(
        [
            new Method
            {
                MethodName = "sIgE",
                MethodNameLis = "LIS-sIgE",
                Tests =
                [
                    new MethodTest
                    {
                        TestName = "t3",
                        FullTestName = "Dog dander",
                        DefaultReportConcentration = true,
                        DefaultReportClassId = 1,
                        DefaultReportCutOff = true,
                        DefaultReportQuotient = QuotientReportMode.ReportAsPercent
                    }
                ]
            }
        ]);
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetSampleTestResultsForLisQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new LisSampleTestResult { TestOrderId = testOrderUid, Pending = true }]);
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetSampleTubePositionsForLisQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new SendTestResultsToLisCommandHandler(
            _dbContextMock.Object,
            methodsService,
            _senderMock.Object,
            _tubeSettingsMock.Object,
            _lisSettingsMock.Object);

        // Act
        await sut.Handle(new SendSampleTestResultsToLisCommand([testOrder.Id]), TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(
            It.Is<Server.Application.Lis.SendSampleTestResultsToLisCommand>(command =>
                command.Patients.Single().Samples.Single().TestOrders.Single().ReportConcentration &&
                command.Patients.Single().Samples.Single().TestOrders.Single().ReportClass &&
                command.Patients.Single().Samples.Single().TestOrders.Single().ReportCutOff &&
                command.Patients.Single().Samples.Single().TestOrders.Single().ReportQuotient &&
                command.Patients.Single().Samples.Single().TestOrders.Single().TestResult!.Pending),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class TestMethodsService(List<Method> methods) : IMethodsService
    {
        public Task<TResult?> GetMethod<TResult>(string methodName, Expression<Func<Method, TResult>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<List<TResult>> GetMethods<TResult>(
            Expression<Func<Method, bool>> filter,
            Expression<Func<Method, TResult>> selector)
        {
            return Task.FromResult(methods
                .Where(filter.Compile())
                .Select(selector.Compile())
                .ToList());
        }

        public Task<List<Method>> GetDetailedMethods()
        {
            throw new NotSupportedException();
        }

        public Task<TResult?> GetMethodTestByLabNameOrTestName<TResult>(string? labMethodName, string labTestName, Expression<Func<MethodTest, TResult>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<TResult?> GetMethodTestByLabName<TResult>(string? labMethodName, string labTestName, Expression<Func<MethodTest, TResult>> selector, bool forceUniqueLaboratoryTestNames = false)
        {
            throw new NotSupportedException();
        }

        public Task<TResult?> GetMethodTest<TResult>(string methodName, string testName, Expression<Func<MethodTest, TResult>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<TResult?> GetQualityControl<TResult>(string methodName, string testName, string qualityControlName, Expression<Func<PhadiaQualityControlTest, TResult>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<TPoint?> GetMethodCalibratorPoint<TPoint>(string methodName, string calibratorName, Expression<Func<MethodCalibrationPoint, TPoint>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<List<TPoint>> GetMethodCalibratorPoints<TPoint>(string methodName, Expression<Func<MethodCalibrationPoint, TPoint>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<(bool PreventDilutionFactorsBelowDefault, int DefaultDilutionFactor, AssayTechnology? Technology)> GetDefaultInstrumentDilutionFactor(string testName, string methodName)
        {
            throw new NotSupportedException();
        }

        public Task<TResult?> GetCurveControl<TResult>(string methodName, string curveControlName, Expression<Func<MethodCurveControl, TResult>> selector)
        {
            throw new NotSupportedException();
        }

        public Task<int> GetCalibrationCurveDaysValidity(string methodName)
        {
            throw new NotSupportedException();
        }

        public Task<int> GetMethodCurveControlCount(string methodName)
        {
            throw new NotSupportedException();
        }

        public Task<HashSet<string>> GetPhadiaQualityControlCartridgeNames()
        {
            throw new NotImplementedException();
        }

        public Task<HashSet<string>> GetQualityControlNames()
        {
            throw new NotSupportedException();
        }

        public Task<string?> GetMethodTestConcentrationUnit(string methodName, string testName)
        {
            throw new NotSupportedException();
        }

        public Task<Dictionary<(string MethodName, string TestName), string?>> GetMethodTestConcentrationUnits(IEnumerable<(string MethodName, string TestName)> methodTestPairs)
        {
            throw new NotSupportedException();
        }

        public Task<Dictionary<string, int?>> GetMethodsSortOrder()
        {
            throw new NotSupportedException();
        }

    }
}
