using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetTestResults;
using Pharos.Server.Result.Domain;
using AnalyticalRun = Pharos.Server.Result.Domain.AnalyticalRun;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;
using MethodCurveControl = Pharos.Server.Domain.Parameters.Methods.MethodCurveControl;
using MethodPresentation = Pharos.Server.Domain.Parameters.Methods.MethodPresentation;
using MethodReporting = Pharos.Server.Domain.Parameters.Methods.MethodReporting;
using MethodTest = Pharos.Server.Domain.Parameters.Methods.MethodTest;
using TestReplicate = Pharos.Server.Result.Domain.TestReplicate;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
//Todo: Extend test when final logic is decided. 
public class GetCurveControlsForChartsQueryTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    private const string _methodName = "sIgE";
    private const string _instrumentNumber = "SEED-0001";
    private readonly DateTimeOffset _dateTimeOffset = new(2024, 6, 1, 0, 0, 0, new TimeSpan(1, 0, 0));

    [Fact]
    public async Task ShouldSelectGivenNrOfPointsWhenAvailable()
    {
        //Arrange
        var testResults = CurveControlTestResultsAllAvailable();

        //Try to fetch 5 number of different runs, for CC-1, around Run with Id: 4. 
        var command = new GetCurveControlsForChartsQuery(4, "CC-1", 5);

        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var resultDbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var parametersDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();

        //Add analyticalRuns and results to db
        resultDbContext.AnalyticalRuns.AddRange(CreateAnalyticalRuns(10));
        resultDbContext.CurveControlTestResults.AddRange(testResults);
        await resultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Add limits to db
        parametersDbContext.Methods.Add(CreateMethod());
        await parametersDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        parametersDbContext.MethodCurveControls.AddRange(CreateMethodCurveControl());
        await parametersDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var linearChartDto = await mediator.Send(command, TestContext.Current.CancellationToken);

        //Assert
        linearChartDto.CurveControls.Should().NotBeNullOrEmpty();

        //Calculate time difference
        var minTime = linearChartDto.CurveControls!.MinBy(x => x.Replicates!.First().MeasurementDateTime)!.Replicates!.First().MeasurementDateTime;
        var maxTime = linearChartDto.CurveControls!.MaxBy(x => x.Replicates!.First().MeasurementDateTime)!.Replicates!.First().MeasurementDateTime;
        var diff = maxTime - minTime;

        linearChartDto.CurveControls!.Count.Should().Be(5);
        diff.TotalDays.Should().BeGreaterOrEqualTo(4);
    }

    [Fact]

    public async Task ShouldSelectMoreThanGivenNrOfPointsUntilTimeCriteriaIsFulfilled()
    {
        //Arrange
        var testResults = CurveControlTestResultsSmallTimeRange();

        //Try to fetch 5 number of different runs, for CC-1, around Run with Id: 4. 
        var command = new GetCurveControlsForChartsQuery(4, "CC-1", 5);

        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var resultDbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var parametersDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();

        //Add analyticalRuns and results to db
        resultDbContext.AnalyticalRuns.AddRange(CreateAnalyticalRuns(10));
        resultDbContext.CurveControlTestResults.AddRange(testResults);
        await resultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Add limits to db
        parametersDbContext.Methods.Add(CreateMethod());
        await parametersDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        parametersDbContext.MethodCurveControls.AddRange(CreateMethodCurveControl());
        await parametersDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var linearChartDto = await mediator.Send(command, TestContext.Current.CancellationToken);

        //Assert
        linearChartDto.CurveControls.Should().NotBeNullOrEmpty();

        //Calculate time difference
        var minTime = linearChartDto.CurveControls!.MinBy(x => x.Replicates!.First().MeasurementDateTime)!.Replicates!.First().MeasurementDateTime;
        var maxTime = linearChartDto.CurveControls!.MaxBy(x => x.Replicates!.First().MeasurementDateTime)!.Replicates!.First().MeasurementDateTime;
        var diff = maxTime - minTime;

        //Time criteria cannot be met, so all entries are returned. 
        linearChartDto.CurveControls!.Count.Should().Be(7);
        diff.TotalDays.Should().BeLessThan(4);
    }

    [Fact]

    public async Task ShouldSelectMoreThanGivenNrOfPointsUntilRunCriteriaIsFulfilled()
    {
        //Arrange
        var testResults = CurveControlTestResultsNonUniqueRuns();

        //Try to fetch 5 number of different runs, for CC-1, around Run with Id: 4. 
        var command = new GetCurveControlsForChartsQuery(4, "CC-1", 5);

        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var resultDbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var parametersDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();

        //Add analyticalRuns and results to db
        resultDbContext.AnalyticalRuns.AddRange(CreateAnalyticalRuns(10));
        resultDbContext.CurveControlTestResults.AddRange(testResults);
        await resultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Add limits to db
        parametersDbContext.Methods.Add(CreateMethod());
        await parametersDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        parametersDbContext.MethodCurveControls.AddRange(CreateMethodCurveControl());
        await parametersDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var linearChartDto = await mediator.Send(command, TestContext.Current.CancellationToken);

        //Assert
        linearChartDto.CurveControls.Should().NotBeNullOrEmpty();

        //Calculate time difference
        var minTime = linearChartDto.CurveControls!.MinBy(x => x.Replicates!.First().MeasurementDateTime)!.Replicates!.First().MeasurementDateTime;
        var maxTime = linearChartDto.CurveControls!.MaxBy(x => x.Replicates!.First().MeasurementDateTime)!.Replicates!.First().MeasurementDateTime;
        var diff = maxTime - minTime;

        //Unique analytical run id criteria cannot be met with just 5 entries, so one more is returned. 
        linearChartDto.CurveControls!.Count.Should().Be(6);
        diff.TotalDays.Should().BeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task ShouldExcludePostCommitCurveControlsWhenRunIsCommitted()
    {
        //Arrange
        var testResults = CurveControlTestResultsAllAvailable();
        var command = new GetCurveControlsForChartsQuery(4, "CC-1", 5);

        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var resultDbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var parametersDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();

        resultDbContext.AnalyticalRuns.AddRange(CreateAnalyticalRuns(10, committedRunId: 4, frozenAt: _dateTimeOffset));
        resultDbContext.CurveControlTestResults.AddRange(testResults);
        await resultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        parametersDbContext.Methods.Add(CreateMethod());
        await parametersDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        parametersDbContext.MethodCurveControls.AddRange(CreateMethodCurveControl());
        await parametersDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        //Act
        var linearChartDto = await mediator.Send(command, TestContext.Current.CancellationToken);

        //Assert
        linearChartDto.CurveControls.Should().NotBeNullOrEmpty();
        linearChartDto.CurveControls!.Should().OnlyContain(x => x.MeasurementDateTime <= _dateTimeOffset);
        linearChartDto.CurveControls.Select(x => x.MeasurementDateTime).Should().NotContain(_dateTimeOffset.AddDays(1));
        linearChartDto.CurveControls.Select(x => x.MeasurementDateTime).Should().NotContain(_dateTimeOffset.AddDays(2));
        linearChartDto.CurveControls.Select(x => x.MeasurementDateTime).Should().NotContain(_dateTimeOffset.AddDays(3));
    }

    private List<CurveControlTestResult> CurveControlTestResultsSmallTimeRange()
    {
        var testResults = new List<CurveControlTestResult>
        {
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 1,
                        MeasurementDateTime = _dateTimeOffset.AddDays(-1.5)
                    }
                },
                AnalyticalRunId = 1
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 2,
                        MeasurementDateTime = _dateTimeOffset.AddDays(-1)
                    }
                },
                AnalyticalRunId = 2
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 3,
                        MeasurementDateTime = _dateTimeOffset.AddDays(-0.5)
                    }
                },
                AnalyticalRunId = 3
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),

                        TestResultId = 4,
                        MeasurementDateTime = _dateTimeOffset
                    }
                },
                AnalyticalRunId = 4
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 5,
                        MeasurementDateTime = _dateTimeOffset.AddDays(0.5)
                    }
                },
                AnalyticalRunId = 5
            },

            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 6,
                        MeasurementDateTime = _dateTimeOffset.AddDays(1)
                    }
                },
                AnalyticalRunId = 6
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 7,
                        MeasurementDateTime = _dateTimeOffset.AddDays(1.5)
                    }
                },
                AnalyticalRunId = 1
            }
        };
        return testResults;
    }

    private List<CurveControlTestResult> CurveControlTestResultsNonUniqueRuns()
    {
        var testResults = new List<CurveControlTestResult>
        {
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 1,
                        MeasurementDateTime = _dateTimeOffset.AddDays(-3)
                    }
                },
                AnalyticalRunId = 1
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 2,
                        MeasurementDateTime = _dateTimeOffset.AddDays(-2)
                    }
                },
                AnalyticalRunId = 2
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 3,
                        MeasurementDateTime = _dateTimeOffset.AddDays(-1)
                    }
                },
                AnalyticalRunId = 4
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),

                        TestResultId = 4,
                        MeasurementDateTime = _dateTimeOffset
                    }
                },
                AnalyticalRunId = 4
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 5,
                        MeasurementDateTime = _dateTimeOffset.AddDays(1)
                    }
                },
                AnalyticalRunId = 5
            },

            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 6,
                        MeasurementDateTime = _dateTimeOffset.AddDays(2)
                    }
                },
                AnalyticalRunId = 6
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 7,
                        MeasurementDateTime = _dateTimeOffset.AddDays(3)
                    }
                },
                AnalyticalRunId = 1
            }
        };
        return testResults;
    }

    private List<CurveControlTestResult> CurveControlTestResultsAllAvailable()
    {
        var testResults = new List<CurveControlTestResult>
        {
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 1,
                        MeasurementDateTime = _dateTimeOffset.AddDays(-3),
                    }
                },
                AnalyticalRunId = 1
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 2,
                        MeasurementDateTime = _dateTimeOffset.AddDays(-2),
                    }
                },
                AnalyticalRunId = 2
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 3,
                        MeasurementDateTime = _dateTimeOffset.AddDays(-1),
                    }
                },
                AnalyticalRunId = 3
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),

                        TestResultId = 4,
                        MeasurementDateTime = _dateTimeOffset,
                    }
                },
                AnalyticalRunId = 4
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 5,
                        MeasurementDateTime = _dateTimeOffset.AddDays(1),
                    }
                },
                AnalyticalRunId = 5
            },

            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 6,
                        MeasurementDateTime = _dateTimeOffset.AddDays(2),
                    }
                },
                AnalyticalRunId = 6
            },
            new()
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = _instrumentNumber,
                MethodName = _methodName,
                TestName = "a-IgE",
                CurveControlPointName = "CC-1",
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        TestResultId = 7,
                        MeasurementDateTime = _dateTimeOffset.AddDays(3),
                    }
                },
                AnalyticalRunId = 1
            }
        };
        return testResults;
    }

    private static List<AnalyticalRun> CreateAnalyticalRuns(int amount, int? committedRunId = null, DateTimeOffset? frozenAt = null)
    {
        var analyticalRuns = new List<AnalyticalRun>();

        for (var i = 0; i < amount; i++)
        {
            var runNumber = i + 1;
            var isCommittedRun = committedRunId == runNumber;

            var run = new AnalyticalRun
            {
                InstrumentAnalyticalRunId = Guid.NewGuid(),
                MethodName = _methodName,
                InstrumentNumber = _instrumentNumber,
                CurveState = isCommittedRun ? CurveState.Frozen : CurveState.Valid,
                FrozenAt = isCommittedRun ? frozenAt : null
            };
            analyticalRuns.Add(run);
        }

        return analyticalRuns;
    }


    private static Method CreateMethod()
    {
        var method = new Method
        {
            MethodName = _methodName,
            FullMethodName = "Full method name",
            AnalyticalRun = new MethodAnalyticalRun(),
            Calculation = new MethodCalculation(),
            Presentation = new MethodPresentation
            {
                XAxisText = "",
                XAxisUnit = "",
                YAxisText = "",
                YAxisUnit = ""
            },
            Reporting = new MethodReporting
            {
                DefaultTextQuantitativeAbove = "",
                DefaultTextQuantitativeBelow = ""
            },
            Tests = new List<MethodTest>
            {
                new()
                {
                    MethodId = 1,
                    TestName = "a-IgE",
                    FullTestName = "",
                    DefaultLabTestName = "",
                    DiluentIdentity = ""
                }
            },
            ConcentrationUnits = new List<MethodConcentrationUnit>
            {
                new()
                {
                    MethodId = 1,
                    ConcentrationUnit = "unit"
                }
            }
        };


        return method;
    }

    private static List<MethodCurveControl> CreateMethodCurveControl()
    {
        return
        [
            new MethodCurveControl
            {
                MethodId = 1,
                MethodTestId = 1,
                CurveControlName = "CC-1"
            }
        ];
    }
}