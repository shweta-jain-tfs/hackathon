using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Lis;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Domain;
using Tetris.Protocol.Instrument.TestOrder;
using SampleTube = Pharos.Server.Sample.Domain.SampleTube;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class TestOrderLisMessageTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    private readonly Fixture _fixture = new();

    [Fact]
    public async Task TestOrderLisMessage_Should_SendTestOrderToInstrument()
    {
        // Arrange
        var sampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength];

        var (instrumentConnectionMock, instrumentState) = await Factory.AddInstrumentConnection();

        var sampleStorage = ServiceProvider.GetRequiredService<IInstrumentSampleStorage>();
        var tube = new SampleTube
        {
            TubeId = sampleId,
            SampleId = sampleId,
            Area = SampleArea.InternalStorage,
            InstrumentNumber = instrumentState.SerialNumber,
            TubePosition = 1
        };
        sampleStorage.AddOrUpdateTube(tube, _ => tube);
        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var paramDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var instrumentDbContext = ServiceProvider.GetRequiredService<IInstrumentDbContext>();


        var sample = new Request.Domain.Request
        {
            SampleId = sampleId
        };
        dbContext.Requests.Add(sample);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sampleDbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        sampleDbContext.SampleTubes.Add(new SampleTube
        {
            TubeId = sampleId,
            SampleId = sampleId,
            RequestCreatedDate = sample.Created,
            InstrumentNumber = instrumentState.SerialNumber
        });

        var fixture = new Fixture();
        var method = new Method
        {
            MethodName = "sIgE",
            FullMethodName = "ImmunoCap Specific IgE 0-100",
            MethodNameLis = "sIgE",
            AnalyticalRun = new MethodAnalyticalRun(),
            Calculation = new MethodCalculation(),
            Presentation = fixture.Create<MethodPresentation>(),
            Reporting = fixture.Create<MethodReporting>(),
            Reagents =
                         [
                             new MethodReagent
                                 {
                                     ReagentType = ReagentType.Conjugate,
                                     DefaultReagentIdentity = "sIgE"
                                 }
                         ],
            ConcentrationUnits =
                         [
                             new MethodConcentrationUnit
                                 {
                                     UnitId = 1,
                                     ConcentrationUnit = "kUA/l"
                                 }
                         ]
        };
        paramDbContext.Methods.Add(method);

        var methodTest = new MethodTest
        {
            TestName = "f1",
            DefaultLabTestName = "f1",
            FullTestName = "Egg white",
            DiluentIdentity = "Dil",
            DefaultConcentrationUnit = method.ConcentrationUnits[0],
            Method = method
        };

        paramDbContext.MethodTests.Add(methodTest);

        var methodLabTest = new MethodTestLabName
        {
            MethodTest = methodTest,
            TestName = methodTest.TestName
        };
        paramDbContext.MethodTestLabNames.Add(methodLabTest);

        // Seed tube type definition
        var settingsDbContext = ServiceProvider.GetRequiredService<ISettingsDbContext>();

        // remove initial settings added by the SettingsDbContextInitializer
        await settingsDbContext.Settings.ToListAsync(TestContext.Current.CancellationToken);
        settingsDbContext.Settings.RemoveRange(settingsDbContext.Settings);

        // Add tube type settings for the instrument
        settingsDbContext.Settings.Add(new Setting
        {
            Key = "Samples:TubeTypes:TubeTypeList:1:Name",
            Value = "Tube Type 4"
        });
        settingsDbContext.Settings.Add(new Setting
        {
            Key = "Samples:TubeTypes:TubeTypeList:1:TubeDefinitionId",
            Value = "1"
        });
        settingsDbContext.Settings.Add(new Setting
        {
            Key = "Samples:TubeTypes:NormalTubeDefinitionId",
            Value = "1"
        });
        await settingsDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add instrument method to make it active
        var instrumentMethod = new Instrument.Domain.InstrumentMethod
        {
            InstrumentId = instrumentState.Id,
            MethodName = "sIgE",
            IsActive = true,
        };
        instrumentDbContext.InstrumentMethods.Add(instrumentMethod);

        await paramDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await sampleDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await instrumentDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var mediator = scope.ServiceProvider.GetService<IMediator>()!;

            var message = new ProcessLisTestOrdersCommand
            {
                LisChannelId = 0
            };

            message.Patients.Add(new LisPatient
            {
                Samples =
                [
                    new LisSample {
                        SampleId = sampleId,
                        TestOrders =
                        [
                            new LisTestOrder
                            {
                                MethodName = "sIgE",
                                TestName = "f1"
                            }
                        ],
                        TubeType = "N"
                    }
                ]
            });

            await mediator.Send(message, CancellationToken.None);
        }

        await WaitForNotificationsAsync();

        // Assert
        instrumentConnectionMock.Verify(x => x.UpdateTestOrderAsync(It.Is<TestOrderRequest>(y => y.Samples.Count == 1 && y.Samples.Single(z => z.Sample.SampleId == sampleId) != null), It.IsAny<CancellationToken>()), Times.Once);
    }
}