using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Parameters.Application.Interfaces;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;
using MethodClassTable = Pharos.Server.Domain.Parameters.Methods.MethodClassTable;
using MethodReporting = Pharos.Server.Domain.Parameters.Methods.MethodReporting;
using MethodTest = Pharos.Server.Domain.Parameters.Methods.MethodTest;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

public class GetTestResultInformationQueryHandlerTests
{
    private readonly Mock<IParametersDbContext> _dbMock = new();

    [Fact]
    public async Task Handle_WhenDefaultNoReportAndUserOverridesToFraction_ShouldReportQuotient()
    {
        var methodTest = CreateMethodTest(QuotientReportMode.NoReport, QuotientReportMode.ReportAsFraction);
        _dbMock.Setup(x => x.MethodTests).ReturnsDbSet([methodTest]);
        var sut = new GetTestResultInformationQueryHandler(_dbMock.Object);

        var result = await sut.Handle(new GetTestResultInformationQuery("fx1", "sIgE"), TestContext.Current.CancellationToken);

        result.ReportQuotient.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenDefaultReportingEnabledAndUserOverridesToNoReport_ShouldNotReportQuotient()
    {
        var methodTest = CreateMethodTest(QuotientReportMode.ReportAsPercent, QuotientReportMode.NoReport);
        _dbMock.Setup(x => x.MethodTests).ReturnsDbSet([methodTest]);
        var sut = new GetTestResultInformationQueryHandler(_dbMock.Object);

        var result = await sut.Handle(new GetTestResultInformationQuery("fx1", "sIgE"), TestContext.Current.CancellationToken);

        result.ReportQuotient.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenNoUserOverride_ShouldRespectDefaultReportQuotient()
    {
        var methodTest = CreateMethodTest(QuotientReportMode.ReportAsFraction, null);
        _dbMock.Setup(x => x.MethodTests).ReturnsDbSet([methodTest]);
        var sut = new GetTestResultInformationQueryHandler(_dbMock.Object);

        var result = await sut.Handle(new GetTestResultInformationQuery("fx1", "sIgE"), TestContext.Current.CancellationToken);

        result.ReportQuotient.Should().BeTrue();
    }

    private static MethodTest CreateMethodTest(QuotientReportMode defaultReportQuotient, QuotientReportMode? userReportQuotient)
    {
        var method = new Method
        {
            MethodName = "sIgE",
            ClassTableBelows = [],
            ClassTables = [new MethodClassTable { ClassTableId = 1, DefaultTextAboveConcentration = "A" }],
            Reporting = new MethodReporting
            {
                DefaultTextAboveCutOff = "Positive",
                DefaultTextBelowCutOff = "Negative"
            }
        };

        return new MethodTest
        {
            TestName = "fx1",
            Method = method,
            MethodId = 1,
            DefaultReportClassId = 1,
            DefaultReportQuotient = defaultReportQuotient,
            UserReportQuotient = userReportQuotient
        };
    }
}
