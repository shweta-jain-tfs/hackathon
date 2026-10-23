using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Reflex;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Request.Application.Commands.CreateUpdateRequest;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;
using Pharos.Server.Request.Application.Queries.GetPatient;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;

namespace Pharos.Server.Request.Application.Commands;

[Collection(nameof(RequestTestsCollection))]
public class UpdateRequestCommandHandlerTests(RequestTestsWaf factory) : DatabaseTestBase<RequestTestsWaf>(factory)
{
    private const int _tubeDefinitionId = 1;
    private const int _tubeDefinitionId2 = 2;
    private const string _method1 = "M1";
    private const string _method2 = "M2";
    private const string _method3 = "M3";
    private const string _methodPreventDilution = "MPRD";
    private const string _test1 = "T1";
    private const string _test2 = "T2";
    private const string _test3 = "T3";
    private const string _testPreventDilution = "TPRD";

    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IAuthorizationService> _authorizationServiceMock = new();
    private readonly Mock<IOptionsSnapshot<RequestSettings>> _requestSettingsMock = new();
    private readonly Mock<IOptionsSnapshot<TubeTypesSettings>> _tubeSettingsMock = new();
    private UpdateRequestCommandHandler _sut = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(true);

        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsPatientInformationEnabled = true,
            IsRequestorInformationEnabled = false,
            AllowEditingOfPredilutionFactor = true,
        });

        _tubeSettingsMock.Setup(x => x.Value).Returns(new TubeTypesSettings
        {
            TubeTypeList =
            [
                new()
                {
                    TubeDefinitionId = _tubeDefinitionId,
                    Name = "Standard",
                    VolumeCalculation = false
                },
                new()
                {
                    TubeDefinitionId = _tubeDefinitionId2,
                    Name = "Standard 2",
                    VolumeCalculation = false
                }
            ],
            NormalTubeDefinitionId = _tubeDefinitionId.ToString()
        });

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SyncTestOrdersWithInstrumentCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetReflexDefinitionsForTestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        try
        {
            await using var scope = ServiceProvider.CreateAsyncScope();
            var parametersDb = scope.ServiceProvider.GetRequiredService<IParametersDbContext>();
            await parametersDb.CreateMethod(methodName: _method1, testNames: [_test1]);
            await parametersDb.CreateMethod(methodName: _method2, testNames: [_test2]);
            await parametersDb.CreateMethod(methodName: _method3, testNames: [_test3]);
        }
        catch
        {
            // ignore duplicate seed attempts between tests
        }

        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var methods = ServiceProvider.GetRequiredService<IMethodsService>();

        _sut = new UpdateRequestCommandHandler(
            _authorizationServiceMock.Object,
            dbContext,
            _requestSettingsMock.Object,
            _tubeSettingsMock.Object,
            _mediatorMock.Object,
            methods);
    }

    /// <summary>
    /// Verify patient age at the date the request was created.
    /// </summary>
    /// <param name="requestDateString"></param>
    /// <param name="birthDateCreatedString"></param>
    /// <param name="birthDateUpdatedString"></param>
    /// <param name="actualAgeAtRequestDate"></param>
    /// <returns></returns>
    [Theory]
    [InlineData("2023-12-31", "2020-01-01", "2010-01-01", 13)]
    [InlineData("2024-01-01", "2020-01-01", "2010-01-01", 14)]
    public async Task UpdateRequest_When_PatientExist_Should_Update_PatientAge(string requestDateString, string birthDateCreatedString, string birthDateUpdatedString, int actualAgeAtRequestDate)
    {
        var requestDate = DateTimeOffset.Parse(requestDateString);
        var birthDateCreated = DateOnly.Parse(birthDateCreatedString);
        var birthDateUpdated = DateOnly.Parse(birthDateUpdatedString);
        var request = await CreateRequest(requestDate, new Patient { PatientId = "PID001", BirthDate = birthDateCreated });

        // Act
        var command = new UpdateRequestCommand
        {
            Id = request.Id,
            RequestDetailsQueriedAt = DateTimeOffset.UtcNow,
            RequestDate = requestDate,
            SampleTubeType = _tubeDefinitionId,
            Patient = new PatientDto
            {
                PatientId = "PID001",
                BirthDate = birthDateUpdated
            },
        };
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        var updatedRequest = await RunInNewScope(dbContext => dbContext.Requests.FirstOrDefaultAsync(x => x.Id == request.Id, TestContext.Current.CancellationToken));
        Assert.NotNull(updatedRequest);
        Assert.Equal(actualAgeAtRequestDate, updatedRequest.PatientAge);
    }

    /// <summary>
    /// Verify patient age at the date the request was created, for other requests.
    /// </summary>
    /// <param name="requestDateString"></param>
    /// <param name="birthDateCreatedString"></param>
    /// <param name="birthDateUpdatedString"></param>
    /// <param name="actualAgeAtRequestDate"></param>
    /// <returns></returns>
    [Theory]
    [InlineData("2023-12-31", "2020-01-01", "2010-01-01", 13)]
    [InlineData("2024-01-01", "2020-01-01", "2010-01-01", 14)]
    public async Task UpdateRequest_When_Patient_Has_OtherRequests_Should_Update_PatientAge(string requestDateString, string birthDateCreatedString, string birthDateUpdatedString, int actualAgeAtRequestDate)
    {
        var requestDate = DateTimeOffset.Parse(requestDateString);
        var birthDateCreated = DateOnly.Parse(birthDateCreatedString);
        var birthDateUpdated = DateOnly.Parse(birthDateUpdatedString);

        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var patient = new Patient { PatientId = "PID001", BirthDate = birthDateCreated };
        var request1 = new Domain.Request
        {
            SampleId = "S1",
            Created = requestDate,
            LastModified = DateTimeOffset.UtcNow,
            PreDilutionFactor = 1,
            SampleTubeType = _tubeDefinitionId,
            SampleVolumeCheck = SampleVolumeCheck.Off,
            Patient = patient
        };
        var request2 = new Domain.Request
        {
            SampleId = "S2",
            Created = requestDate,
            LastModified = DateTimeOffset.UtcNow,
            PreDilutionFactor = 1,
            SampleTubeType = _tubeDefinitionId,
            SampleVolumeCheck = SampleVolumeCheck.Off,
            Patient = patient
        };
        dbContext.Requests.Add(request1);
        dbContext.Requests.Add(request2);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var command = new UpdateRequestCommand
        {
            Id = request1.Id,
            RequestDetailsQueriedAt = DateTimeOffset.UtcNow,
            RequestDate = requestDate,
            SampleTubeType = _tubeDefinitionId,
            Patient = new PatientDto
            {
                PatientId = "PID001",
                BirthDate = birthDateUpdated
            },
        };
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        var updatedPatient = await RunInNewScope(db => db.Patients.Include(p => p.Requests).FirstAsync(p => p.PatientId == "PID001", TestContext.Current.CancellationToken));
        var patientRequests = updatedPatient.Requests!.ToList();
        Assert.NotNull(patientRequests);
        Assert.Equal(2, patientRequests.Count); // Assert that there are exactly 2 requests.

        foreach (var patientRequest in patientRequests)
        {
            Assert.Equal(actualAgeAtRequestDate, patientRequest.PatientAge);
        }
    }

    /// <summary>
    /// Verify (a new patient) age at the date the request was created.
    /// </summary>
    /// <param name="requestDateString"></param>
    /// <param name="birthDateString"></param>
    /// <param name="actualAgeAtRequestDate"></param>
    /// <returns></returns>
    [Theory]
    [InlineData("2023-12-31", "2010-01-01", 13)]
    [InlineData("2024-01-01", "2010-01-01", 14)]
    public async Task UpdateRequest_When_PatientNotExist_Should_Update_PatientAge(string requestDateString, string birthDateString, int actualAgeAtRequestDate)
    {
        var requestDate = DateTimeOffset.Parse(requestDateString);
        var birthDate = DateOnly.Parse(birthDateString);
        var request = await CreateRequest(requestDate);

        // Act
        var command = new UpdateRequestCommand
        {
            Id = request.Id,
            RequestDetailsQueriedAt = DateTimeOffset.UtcNow,
            RequestDate = requestDate,
            SampleTubeType = _tubeDefinitionId,
            Patient = new PatientDto
            {
                PatientId = "PID001",
                BirthDate = birthDate
            },
        };
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        var updatedRequest = await RunInNewScope(dbContext => dbContext.Requests.FirstOrDefaultAsync(x => x.Id == request.Id, TestContext.Current.CancellationToken));
        Assert.NotNull(updatedRequest);
        Assert.Equal(actualAgeAtRequestDate, updatedRequest.PatientAge);
    }

    [Fact]
    public async Task UpdateRequest_Should_Remove_ReflexDefinition_When_Removed_From_Update()
    {
        var reflexDef1 = new ReflexDefinition
        {
            Name = "Reflex 1",
            ReflexCondition = new ReflexCondition { MethodName = _method1, TestName = _test1 }
        };
        var reflexDef2 = new ReflexDefinition
        {
            Name = "Reflex 2",
            ReflexCondition = new ReflexCondition { MethodName = _method1, TestName = _test1 }
        };
        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            LastModified = DateTimeOffset.UtcNow,
            MethodName = _method1,
            TestName = _test1,
            InstrumentDilutionFactor = 1,
            NumberOfReplicates = 1,
            TestPanelIds = [],
            ReflexDefinitions = new List<ReflexDefinition> { reflexDef1, reflexDef2 }
        };
        var request = await CreateRequest(DateTimeOffset.UtcNow, null, [testOrder]);

        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var storedTestOrder = await dbContext.Requests
            .Where(x => x.Id == request.Id)
            .SelectMany(x => x.TestOrders)
            .FirstAsync(TestContext.Current.CancellationToken);

        var reflexIdToKeep = storedTestOrder.ReflexDefinitions!.First().Id;
        var updatedTest = new NewUpdatedTest
        {
            Id = storedTestOrder.Id,
            ReflexDefinitionIds = [reflexIdToKeep],
            TestPanelIds = []
        };
        var command = new UpdateRequestCommand
        {
            Id = request.Id,
            RequestDetailsQueriedAt = DateTimeOffset.UtcNow,
            TestOrders = [updatedTest],
            PreDilutionFactor = request.PreDilutionFactor,
            SampleTubeType = request.SampleTubeType
        };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        var updatedRequest = await RunInNewScope(db => db.Requests
            .Where(x => x.Id == request.Id)
            .Include(x => x.TestOrders)
            .ThenInclude(x => x.ReflexDefinitions)
            .FirstAsync());
        Assert.Single(updatedRequest.TestOrders[0].ReflexDefinitions!);
        Assert.Equal(reflexIdToKeep, updatedRequest.TestOrders[0].ReflexDefinitions![0].Id);
    }

    [Fact]
    public async Task UpdateRequest_Should_Not_Add_New_ReflexDefinition_That_Was_Not_Present_Before()
    {
        var reflexDef1 = new ReflexDefinition
        {
            Name = "Reflex 1",
            ReflexCondition = new ReflexCondition { MethodName = _method1, TestName = _test1 }
        };
        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            LastModified = DateTimeOffset.UtcNow,
            MethodName = _method1,
            TestName = _test1,
            InstrumentDilutionFactor = 1,
            NumberOfReplicates = 1,
            TestPanelIds = [],
            ReflexDefinitions = new List<ReflexDefinition> { reflexDef1 }
        };
        var request = await CreateRequest(DateTimeOffset.UtcNow, null, [testOrder]);

        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var storedTestOrder = await dbContext.Requests
            .Where(x => x.Id == request.Id)
            .SelectMany(x => x.TestOrders)
            .FirstAsync(TestContext.Current.CancellationToken);

        var existingReflexId = storedTestOrder.ReflexDefinitions!.Single().Id;
        var updatedTest = new NewUpdatedTest
        {
            Id = storedTestOrder.Id,
            ReflexDefinitionIds = [existingReflexId, existingReflexId + 1000],
            TestPanelIds = []
        };
        var command = new UpdateRequestCommand
        {
            Id = request.Id,
            RequestDetailsQueriedAt = DateTimeOffset.UtcNow,
            TestOrders = [updatedTest],
            PreDilutionFactor = request.PreDilutionFactor,
            SampleTubeType = request.SampleTubeType
        };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        var updatedRequest = await RunInNewScope(db => db.Requests
            .Where(x => x.Id == request.Id)
            .Include(x => x.TestOrders)
            .ThenInclude(x => x.ReflexDefinitions)
            .FirstAsync());
        Assert.Single(updatedRequest.TestOrders[0].ReflexDefinitions!);
        Assert.Equal(existingReflexId, updatedRequest.TestOrders[0].ReflexDefinitions![0].Id);
    }

    [Fact]
    public async Task UpdateRequest_When_PatientExists_Should_SetPatientAge()
    {
        var request = await CreateRequest(DateTimeOffset.Parse("2025-10-06"));
        var birthDate = DateOnly.Parse("1982-01-19");
        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var patient = new Patient { PatientId = "PID001", BirthDate = birthDate };
        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var command = new UpdateRequestCommand
        {
            Id = request.Id,
            RequestDetailsQueriedAt = DateTimeOffset.UtcNow,
            SampleTubeType = _tubeDefinitionId,
            Patient = new PatientDto
            {
                PatientId = "PID001",
                BirthDate = birthDate
            }
        };
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        var createdRequest = await RunInNewScope(db => db.Requests.FirstOrDefaultAsync(x => x.Id == request.Id, TestContext.Current.CancellationToken));
        createdRequest.Should().NotBeNull();
        createdRequest.PatientAge.Should().Be(43);
    }

    [Fact]
    public async Task UpdateRequest_When_PatientDoesNotExists_Should_SetPatientAge()
    {
        var request = await CreateRequest(DateTimeOffset.Parse("2025-10-06"));

        var birthDate = DateOnly.Parse("1982-01-19");

        // Act
        var command = new UpdateRequestCommand
        {
            Id = request.Id,
            RequestDetailsQueriedAt = DateTimeOffset.UtcNow,
            SampleTubeType = _tubeDefinitionId,
            Patient = new PatientDto
            {
                PatientId = "PID001",
                BirthDate = birthDate
            }
        };
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        var createdRequest = await RunInNewScope(dbContext => dbContext.Requests.FirstOrDefaultAsync(x => x.Id == request.Id, TestContext.Current.CancellationToken));
        createdRequest.Should().NotBeNull();
        createdRequest.PatientAge.Should().Be(43);
    }

    [Fact]
    public async Task UpdateRequest_When_PatientExists_AndPatientBirthDateHasBeenChanged_Should_SetPatientAge()
    {
        var request = await CreateRequest(DateTimeOffset.Parse("2025-10-06"));

        var birthDate = DateOnly.Parse("1982-01-19");
        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var patient = new Patient { PatientId = "PID001", BirthDate = birthDate };
        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var command = new UpdateRequestCommand
        {
            Id = request.Id,
            RequestDetailsQueriedAt = DateTimeOffset.UtcNow,
            SampleTubeType = _tubeDefinitionId,
            Patient = new PatientDto
            {
                PatientId = "PID001",
                BirthDate = birthDate.AddYears(1) // <-- Change birthdate to one year later
            }
        };
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        var createdRequest = await RunInNewScope(db => db.Requests.FirstOrDefaultAsync(x => x.Id == request.Id, TestContext.Current.CancellationToken));
        createdRequest.Should().NotBeNull();
        createdRequest.PatientAge.Should().Be(42);
    }

    #region PreDilution validation

    [Fact]
    public async Task Handle_WhenPreDilutionFactorDecreases_ShouldRevalidateExistingTests_AndThrow()
    {
        // Arrange: create a method that prevents combined dilution below minimum
        await using var setupScope = ServiceProvider.CreateAsyncScope();
        var parametersDb = setupScope.ServiceProvider.GetRequiredService<IParametersDbContext>();
        var method = await parametersDb.CreateMethod(methodName: _methodPreventDilution, testNames: [_testPreventDilution]);
        method.PreventDilutionFactorsBelowDefault = true;
        method.Tests[0].DefaultDilutionFactor = 4;
        method.Tests[0].DefaultDilutionMethod = DilutionMethod.InstrumentDilution;
        await parametersDb.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Create request with PreDilutionFactor = 4 and InstrumentDilutionFactor = 1 (combined = 4 = minimum)
        var request = await CreateRequest(DateTimeOffset.UtcNow, null,
        [
            new TestOrder
            {
                Uid = Guid.NewGuid(),
                LastModified = DateTimeOffset.UtcNow,
                MethodName = _methodPreventDilution,
                TestName = _testPreventDilution,
                InstrumentDilutionFactor = 1,
                NumberOfReplicates = 1,
                TestPanelIds = [],
                TestSortOrder = 1,
                State = TestOrderState.NotStarted
            }
        ]);
        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        request.PreDilutionFactor = 4;
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var requestWithTests = await RunInNewScope(db => db.Requests
            .Where(r => r.Id == request.Id)
            .Include(r => r.TestOrders)
            .FirstAsync());

        // Decrease pre-dilution: combined = 1 * 1 = 1, which is below the minimum of 4
        var command = BuildUpdateCommand(requestWithTests, [CreateUpdatedTest(requestWithTests.TestOrders[0])]);
        command.PreDilutionFactor = 1;

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => _sut.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_WhenPreDilutionFactorUnchanged_ShouldSkipValidation_ForExistingTestWithUnchangedDilutionFactor()
    {
        // Arrange: create a method that prevents combined dilution below minimum
        await using var setupScope = ServiceProvider.CreateAsyncScope();
        var parametersDb = setupScope.ServiceProvider.GetRequiredService<IParametersDbContext>();
        var method = await parametersDb.CreateMethod(methodName: _methodPreventDilution, testNames: [_testPreventDilution]);
        method.PreventDilutionFactorsBelowDefault = true;
        method.Tests[0].DefaultDilutionFactor = 4;
        method.Tests[0].DefaultDilutionMethod = DilutionMethod.InstrumentDilution;
        await parametersDb.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Create request directly with PreDilutionFactor = 1 and InstrumentDilutionFactor = 1
        // Combined = 1 * 1 = 1, below the minimum of 4, simulating legacy data
        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var requestEntity = new Domain.Request
        {
            SampleId = $"S{Guid.NewGuid():N}"[..10],
            Created = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
            PreDilutionFactor = 1,
            SampleTubeType = _tubeDefinitionId,
            SampleVolumeCheck = SampleVolumeCheck.Off,
            TestOrders =
            [
                new TestOrder
                {
                    Uid = Guid.NewGuid(),
                    LastModified = DateTimeOffset.UtcNow,
                    MethodName = _methodPreventDilution,
                    TestName = _testPreventDilution,
                    InstrumentDilutionFactor = 1,
                    NumberOfReplicates = 1,
                    TestPanelIds = [],
                    TestSortOrder = 1,
                    State = TestOrderState.NotStarted
                }
            ]
        };
        dbContext.Requests.Add(requestEntity);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var requestWithTests = await RunInNewScope(db => db.Requests
            .Where(r => r.Id == requestEntity.Id)
            .Include(r => r.TestOrders)
            .FirstAsync());

        // Update with identical PreDilutionFactor and InstrumentDilutionFactor — preDilutionChanged = false
        var command = BuildUpdateCommand(requestWithTests, [CreateUpdatedTest(requestWithTests.TestOrders[0])]);

        // Act: should not throw because validation is skipped for the unchanged existing test
        await _sut.Handle(command, TestContext.Current.CancellationToken);
    }

    #endregion

    #region Test order scheduling

    [Theory]
    [InlineData("NumberOfReplicates")]
    [InlineData("InstrumentDilutionFactor")]
    [InlineData("TestSortOrder")]
    public async Task Handle_Should_Send_SyncCommand_When_ExistingTestOrderIsModified(string changedField)
    {
        var request = await CreateRequestWithTwoNotStartedTests();

        var updatedTest1 = CreateUpdatedTest(request.TestOrders[0]);
        switch (changedField)
        {
            case "NumberOfReplicates":
                updatedTest1.NumberOfReplicates = request.TestOrders[0].NumberOfReplicates + 1;
                break;
            case "InstrumentDilutionFactor":
                updatedTest1.InstrumentDilutionFactor = request.TestOrders[0].InstrumentDilutionFactor + 1;
                break;
            case "TestSortOrder":
                updatedTest1.TestSortOrder = request.TestOrders[0].TestSortOrder + 10;
                break;
        }

        var command = BuildUpdateCommand(request, [
            updatedTest1,
            CreateUpdatedTest(request.TestOrders[1])
        ]);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _mediatorMock.Verify(
            m => m.Send(
                It.Is<SyncTestOrdersWithInstrumentCommand>(c =>
                    c.Request.Id == request.Id &&
                    (c.DeletedTestOrders == null || c.DeletedTestOrders.Count == 0)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Not_Delete_TestOrders_Added_After_RequestDetailsWereQueried_When_NotSentByClient()
    {
        var request = await CreateRequest(DateTimeOffset.UtcNow.AddMinutes(-5), null,
        [
            new TestOrder
            {
                Uid = Guid.NewGuid(),
                MethodName = _method1,
                TestName = _test1,
                TestSortOrder = 1,
                State = TestOrderState.NotStarted
            }
        ]);

        var requestSnapshot = await RunInNewScope(db => db.Requests
            .Where(r => r.Id == request.Id)
            .Include(r => r.TestOrders)
            .FirstAsync());

        var requestDetailsQueriedAt = DateTimeOffset.UtcNow;
        var concurrentlyAddedTestUid = Guid.NewGuid();

        await RunInNewScope(async db =>
        {
            var requestToExtend = await db.Requests
                .Where(r => r.Id == request.Id)
                .Include(r => r.TestOrders)
                .FirstAsync();

            requestToExtend.TestOrders.Add(new TestOrder
            {
                Uid = concurrentlyAddedTestUid,
                MethodName = _method2,
                TestName = _test2,
                TestSortOrder = 2,
                State = TestOrderState.NotStarted
            });

            await db.SaveChangesAsync();
            return true;
        });

        ServiceProvider.GetRequiredService<IRequestDbContext>().ClearChanges();

        var command = BuildUpdateCommand(requestSnapshot,
        [
            CreateUpdatedTest(requestSnapshot.TestOrders.Single())
        ]);
        command.RequestDetailsQueriedAt = requestDetailsQueriedAt;

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        var updatedRequest = await RunInNewScope(db => db.Requests
            .Where(r => r.Id == request.Id)
            .Include(r => r.TestOrders)
            .FirstAsync());

        Assert.Equal(2, updatedRequest.TestOrders.Count);
        Assert.Contains(updatedRequest.TestOrders, x => x.Uid == concurrentlyAddedTestUid);

        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<INotification>(n => n.GetType().Name == "TestOrdersDeletedEvent"),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Throw_ConflictException_When_ExistingTestWasDeletedByAnotherUser()
    {
        var request = await CreateRequest(DateTimeOffset.UtcNow, null,
        [
            new TestOrder
            {
                Uid = Guid.NewGuid(),
                MethodName = _method1,
                TestName = _test1,
                TestSortOrder = 1,
                State = TestOrderState.NotStarted
            }
        ]);

        var requestSnapshot = await RunInNewScope(db => db.Requests
            .Where(r => r.Id == request.Id)
            .Include(r => r.TestOrders)
            .FirstAsync());

        await RunInNewScope(async db =>
        {
            var requestToChange = await db.Requests
                .Where(r => r.Id == request.Id)
                .Include(r => r.TestOrders)
                .FirstAsync();

            db.TestOrders.Remove(requestToChange.TestOrders.Single());
            await db.SaveChangesAsync();
            return true;
        });

        ServiceProvider.GetRequiredService<IRequestDbContext>().ClearChanges();

        var command = BuildUpdateCommand(requestSnapshot,
        [
            CreateUpdatedTest(requestSnapshot.TestOrders.Single())
        ]);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.Handle(command, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(TestOrderState.NotStarted, nameof(NewUpdatedTest.NumberOfReplicates), false)]
    [InlineData(TestOrderState.NotStarted, nameof(NewUpdatedTest.InstrumentDilutionFactor), false)]
    [InlineData(TestOrderState.Scheduled, nameof(NewUpdatedTest.NumberOfReplicates), false)]
    [InlineData(TestOrderState.Scheduled, nameof(NewUpdatedTest.InstrumentDilutionFactor), false)]
    [InlineData(TestOrderState.UnableToStart, nameof(NewUpdatedTest.NumberOfReplicates), false)]
    [InlineData(TestOrderState.UnableToStart, nameof(NewUpdatedTest.InstrumentDilutionFactor), false)]
    [InlineData(TestOrderState.Started, nameof(NewUpdatedTest.NumberOfReplicates), true)]
    [InlineData(TestOrderState.Started, nameof(NewUpdatedTest.InstrumentDilutionFactor), true)]
    [InlineData(TestOrderState.Reported, nameof(NewUpdatedTest.NumberOfReplicates), true)]
    [InlineData(TestOrderState.Reported, nameof(NewUpdatedTest.InstrumentDilutionFactor), true)]
    public async Task Handle_Should_Allow_Or_Throw_When_Updating_Properties_BasedOn_TestOrderState(TestOrderState state, string propertyName, bool shouldThrow)
    {
        var request = await CreateRequest(DateTimeOffset.UtcNow, null,
        [
            new TestOrder
            {
                Uid = Guid.NewGuid(),
                LastModified = DateTimeOffset.UtcNow,
                MethodName = _method1,
                TestName = _test1,
                InstrumentDilutionFactor = 1,
                NumberOfReplicates = 1,
                TestPanelIds = [],
                TestSortOrder = 1,
                State = state
            }
        ]);

        var requestWithTests = await RunInNewScope(db => db.Requests
            .Where(r => r.Id == request.Id)
            .Include(r => r.TestOrders)
            .FirstAsync());

        var originalTest = requestWithTests.TestOrders[0];
        var updatedTest1 = CreateUpdatedTest(originalTest);
        var property = typeof(NewUpdatedTest).GetProperty(propertyName);
        var currentValue = (int)property!.GetValue(updatedTest1)!;
        var expectedValue = currentValue + 1;
        property.SetValue(updatedTest1, expectedValue);

        var command = BuildUpdateCommand(requestWithTests,
        [
            updatedTest1
        ]);

        if (shouldThrow)
        {
            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => _sut.Handle(command, TestContext.Current.CancellationToken));
            return;
        }

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        var updatedTest = await RunInNewScope(db => db.TestOrders
            .Where(r => r.Uid == originalTest.Uid)
            .FirstAsync());

        var updatedProperty = typeof(TestOrder).GetProperty(propertyName);
        var updatedValue = (int)updatedProperty!.GetValue(updatedTest)!;

        Assert.Equal(expectedValue, updatedValue);
    }

    [Fact]
    public async Task Handle_Should_Send_SyncCommand_WithDeletedTestOrders_When_TestOrderIsRemoved()
    {
        var request = await CreateRequestWithTwoNotStartedTests();
        var deletedTestOrder = request.TestOrders[1];

        var command = BuildUpdateCommand(request, [
            CreateUpdatedTest(request.TestOrders[0])
        ]);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _mediatorMock.Verify(
            m => m.Send(
                It.Is<SyncTestOrdersWithInstrumentCommand>(c =>
                    c.Request.Id == request.Id &&
                    c.DeletedTestOrders != null &&
                    c.DeletedTestOrders.Count == 1 &&
                    c.DeletedTestOrders[0].Uid == deletedTestOrder.Uid),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<RequestUpdatedEvent>(e =>
                    e.RequestId == request.Id
                    && e.TestOrders.All(testOrder => testOrder.Uid != deletedTestOrder.Uid)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Send_SyncCommand_When_TestOrderIsAdded()
    {
        var request = await CreateRequestWithTwoNotStartedTests();

        var command = BuildUpdateCommand(request,
        [
            CreateUpdatedTest(request.TestOrders[0]),
            CreateUpdatedTest(request.TestOrders[1]),
            new NewUpdatedTest
            {
                Id = 0,
                MethodName = _method3,
                TestName = _test3,
                InstrumentDilutionFactor = 1,
                NumberOfReplicates = 1,
                TestSortOrder = 3,
                TestPanelIds = [],
                ReflexDefinitionIds = []
            }
        ]);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _mediatorMock.Verify(
            m => m.Send(
                It.Is<SyncTestOrdersWithInstrumentCommand>(c =>
                    c.Request.Id == request.Id &&
                    c.Request.TestOrders.Count == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("PreDilutionFactor")]
    [InlineData("HighPriority")]
    [InlineData("SampleTubeType")]
    public async Task Handle_Should_Send_SyncCommand_When_TubePropertiesAreChanged(string changedProperty)
    {
        var request = await CreateRequestWithTwoNotStartedTests();

        var command = BuildUpdateCommand(request, [
            CreateUpdatedTest(request.TestOrders[0]),
            CreateUpdatedTest(request.TestOrders[1])
        ]);
        switch (changedProperty)
        {
            case "PreDilutionFactor":
                command.PreDilutionFactor = request.PreDilutionFactor + 1;
                break;
            case "HighPriority":
                command.HighPriority = !request.HighPriority;
                break;
            case "SampleTubeType":
                command.SampleTubeType = _tubeDefinitionId2;
                break;
        }

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _mediatorMock.Verify(
            m => m.Send(It.IsAny<SyncTestOrdersWithInstrumentCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    private async Task<Domain.Request> CreateRequest(DateTimeOffset created, Patient? patient = null, List<TestOrder>? testOrders = null)
    {
        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        var request = new Domain.Request
        {
            SampleId = $"S{Guid.NewGuid():N}"[..10],
            Created = created,
            LastModified = DateTimeOffset.UtcNow,
            PreDilutionFactor = 1,
            SampleTubeType = _tubeDefinitionId,
            SampleVolumeCheck = SampleVolumeCheck.Off,
            Patient = patient,
            TestOrders = testOrders ?? []
        };

        dbContext.Requests.Add(request);
        await dbContext.SaveChangesAsync();
        return request;
    }

    private async Task<Domain.Request> CreateRequestWithTwoNotStartedTests()
    {
        var request = await CreateRequest(DateTimeOffset.UtcNow, null,
        [
            new TestOrder
            {
                Uid = Guid.NewGuid(),
                LastModified = DateTimeOffset.UtcNow,
                MethodName = _method1,
                TestName = _test1,
                InstrumentDilutionFactor = 1,
                NumberOfReplicates = 1,
                TestPanelIds = [],
                TestSortOrder = 1,
                State = TestOrderState.NotStarted
            },
            new TestOrder
            {
                Uid = Guid.NewGuid(),
                LastModified = DateTimeOffset.UtcNow,
                MethodName = _method2,
                TestName = _test2,
                InstrumentDilutionFactor = 1,
                NumberOfReplicates = 1,
                TestPanelIds = [],
                TestSortOrder = 2,
                State = TestOrderState.NotStarted
            }
        ]);

        var dbContext = ServiceProvider.GetRequiredService<IRequestDbContext>();
        return await dbContext.Requests
            .Where(r => r.Id == request.Id)
            .Include(r => r.TestOrders)
            .FirstAsync();
    }

    private async Task<T> RunInNewScope<T>(Func<IRequestDbContext, Task<T>> action)
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IRequestDbContext>();
        return await action(dbContext);
    }

    private static UpdateRequestCommand BuildUpdateCommand(Domain.Request request, List<NewUpdatedTest> tests)
    {
        return new UpdateRequestCommand
        {
            Id = request.Id,
            RequestDetailsQueriedAt = DateTimeOffset.UtcNow,
            RequestDate = request.RequestDate,
            SampleCollectionDate = request.SampleCollectionDate,
            RequestId = request.RequestId,
            SampleVolumeCheck = request.SampleVolumeCheck,
            PreDilutionFactor = request.PreDilutionFactor,
            SampleTubeType = request.SampleTubeType,
            HighPriority = request.HighPriority,
            TestOrders = tests
        };
    }

    private static NewUpdatedTest CreateUpdatedTest(TestOrder testOrder)
    {
        return new NewUpdatedTest
        {
            Id = testOrder.Id,
            MethodName = testOrder.MethodName,
            TestName = testOrder.TestName,
            InstrumentDilutionFactor = testOrder.InstrumentDilutionFactor,
            NumberOfReplicates = testOrder.NumberOfReplicates,
            TestPanelIds = testOrder.TestPanelIds,
            TestSortOrder = testOrder.TestSortOrder,
            ReflexDefinitionIds = testOrder.ReflexDefinitions?.Select(r => r.Id).ToList()
        };
    }
}
