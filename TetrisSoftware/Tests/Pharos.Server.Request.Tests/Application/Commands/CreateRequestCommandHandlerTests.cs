using FluentValidation;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Request.Application.Commands.CreateUpdateRequest;
using Pharos.Server.Request.Application.Queries.GetPatient;

namespace Pharos.Server.Request.Application.Commands;

public class CreateRequestCommandHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock;
    private readonly CreateRequestCommandHandler _sut;
    private readonly Mock<IMediator> _mediatorMock;

    public CreateRequestCommandHandlerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _dbContextMock = new Mock<IRequestDbContext>();

        // Add mock for IAuthorizationService, and simulate that the current user has the required permission
        var authorizationServiceMock = new Mock<IAuthorizationService>();
        authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(true);

        // Add mock for IOptionsSnapshot<RequestSettings>
        var requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsPatientInformationEnabled = true
        });

        var tubeSettingsMock = new Mock<IOptionsSnapshot<TubeTypesSettings>>();
        tubeSettingsMock.Setup(x => x.Value).Returns(new TubeTypesSettings
        {
            NormalTubeDefinitionId = "1",
            TubeTypeList =
            [
                new()
                {
                    TubeDefinitionId = 1,
                    Name = "Standard",
                    VolumeCalculation = false
                }
            ]
        });

        _sut = new CreateRequestCommandHandler(
            authorizationServiceMock.Object,
            _dbContextMock.Object,
            requestSettingsMock.Object,
            tubeSettingsMock.Object,
            _mediatorMock.Object,
            new Mock<IMethodsService>().Object);
    }

    /// <summary>
    /// Verify patient age at sample collection date (or request date as fallback).
    /// </summary>
    /// <param name="requestDateString"></param>
    /// <param name="birthDateCreatedString"></param>
    /// <param name="birthDateUpdatedString"></param>
    /// <param name="actualAgeAtRequestDate"></param>
    /// <returns></returns>
    [Theory]
    [InlineData("2023-12-31", "2020-01-01", "2010-01-01", 13)]
    [InlineData("2024-01-01", "2020-01-01", "2010-01-01", 14)]
    public async Task CreateRequest_When_PatientExist_Should_Update_PatientAge(string requestDateString, string birthDateCreatedString, string birthDateUpdatedString, int actualAgeAtRequestDate)
    {
        // Arrange..
        var requestDate = DateTimeOffset.Parse(requestDateString);
        var birthDateCreated = DateOnly.Parse(birthDateCreatedString);
        var birthDateUpdated = DateOnly.Parse(birthDateUpdatedString);

        // ..setup requests.
        var requests = new List<Domain.Request>();
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);
        _dbContextMock.Setup(x => x.Requests.Add(It.IsAny<Domain.Request>())).Callback<Domain.Request>(r => requests.Add(r));

        // ..setup patients.
        var patient = new Patient
        {
            PatientId = "PID001",
            BirthDate = birthDateCreated,
            Requests = requests,
        };
        var patients = new List<Patient> { patient };
        _dbContextMock.Setup(x => x.Patients).ReturnsDbSet(patients);

        _mediatorMock.Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));

        // Act
        var command = new CreateRequestCommand
        {
            RequestDate = requestDate,
            SampleTubeType = 1,
            Patient = new PatientDto
            {
                PatientId = "PID001",
                BirthDate = birthDateUpdated
            },
        };
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        var createdRequest = requests.FirstOrDefault();
        Assert.NotNull(createdRequest);
        Assert.Equal(actualAgeAtRequestDate, createdRequest.PatientAge);
    }

    [Theory]
    [InlineData("2023-12-31", "2020-01-01", 3)]
    [InlineData("2024-01-01", "2020-01-01", 4)]
    public async Task CreateRequest_When_PatientNotExist_Should_Update_PatientAge(string requestDateString, string birthDateString, int actualAgeAtRequestDate)
    {
        // Arrange..
        var requestDate = DateTimeOffset.Parse(requestDateString);
        var birthDate = DateOnly.Parse(birthDateString);

        // ..setup requests.
        var requests = new List<Domain.Request>();
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);
        _dbContextMock.Setup(x => x.Requests.Add(It.IsAny<Domain.Request>())).Callback<Domain.Request>(r => requests.Add(r));

        // ..setup patients.
        _dbContextMock.Setup(x => x.Patients).ReturnsDbSet(new List<Patient>());

        _mediatorMock.Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));

        // Act
        var command = new CreateRequestCommand
        {
            RequestDate = requestDate,
            SampleTubeType = 1,
            Patient = new PatientDto
            {
                PatientId = "PID001",
                BirthDate = birthDate
            }
        };
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        var createdRequest = requests.FirstOrDefault();
        Assert.NotNull(createdRequest);
        Assert.Equal(actualAgeAtRequestDate, createdRequest.PatientAge);
    }

    /// <summary>
    /// Verify patient age at sample collection date (or request date as fallback), for other requests.
    /// </summary>
    /// <param name="requestDateString"></param>
    /// <param name="birthDateCreatedString"></param>
    /// <param name="birthDateUpdatedString"></param>
    /// <param name="actualAgeAtRequestDate"></param>
    /// <returns></returns>
    [Theory]
    [InlineData("2023-12-31", "2020-01-01", "2010-01-01", 13)]
    [InlineData("2024-01-01", "2020-01-01", "2010-01-01", 14)]
    public async Task CreateRequest_When_Patient_Has_OtherRequests_Should_Update_PatientAge(string requestDateString, string birthDateCreatedString, string birthDateUpdatedString, int actualAgeAtRequestDate)
    {
        // Arrange..
        var requestDate = DateTimeOffset.Parse(requestDateString);
        var birthDateCreated = DateOnly.Parse(birthDateCreatedString);
        var birthDateUpdated = DateOnly.Parse(birthDateUpdatedString);

        // ..setup requests.
        var request = new Domain.Request
        {
            Id = 11,
            Created = requestDate,
        };
        var requests = new List<Domain.Request> { request };
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);
        _dbContextMock.Setup(x => x.Requests.Add(It.IsAny<Domain.Request>())).Callback<Domain.Request>(r => requests.Add(r));


        // ..setup patients.
        var patient = new Patient
        {
            PatientId = "PID001",
            BirthDate = birthDateCreated,
            Requests = requests,
        };
        var patients = new List<Patient> { patient };
        _dbContextMock.Setup(x => x.Patients).ReturnsDbSet(patients);

        _mediatorMock.Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));

        // Act
        var command = new CreateRequestCommand
        {
            RequestDate = requestDate,
            SampleTubeType = 1,
            Patient = new PatientDto
            {
                PatientId = "PID001",
                BirthDate = birthDateUpdated
            },
        };
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        var patientRequests = patient.Requests.ToList();
        Assert.NotNull(patientRequests);
        Assert.Equal(2, patientRequests.Count); // Assert that there are exactly 2 requests.

        foreach (var patientRequest in patientRequests)
        {
            Assert.Equal(actualAgeAtRequestDate, patientRequest.PatientAge);
        }
    }

    [Fact]
    public async Task CreateRequest_When_PatientExists_Should_SetPatientAge()
    {
        // Arrange
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));
        var requests = new List<Domain.Request>();
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);
        _dbContextMock.Setup(x => x.Requests.Add(It.IsAny<Domain.Request>())).Callback<Domain.Request>(r => requests.Add(r));

        var requestDate = DateTimeOffset.Parse("2025-10-06");
        var birthDate = DateOnly.Parse("1982-01-19");
        var patient = new Patient
        {
            PatientId = "PID001",
            BirthDate = birthDate
        };
        _dbContextMock.Setup(x => x.Patients).ReturnsDbSet(new List<Patient> { patient });

        // Act
        var command = new CreateRequestCommand
        {
            RequestDate = requestDate,
            SampleTubeType = 1,
            Patient = new PatientDto
            {
                PatientId = "PID001",
                BirthDate = birthDate
            }
        };
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        var createdRequest = _dbContextMock.Object.Requests.FirstOrDefault();
        createdRequest.Should().NotBeNull();
        createdRequest.PatientAge.Should().Be(43);
    }

    [Fact]
    public async Task CreateRequest_Should_CalculateAgeFromProvidedBirthDate_WhenPatientIsNew()
    {
        // Arrange
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));
        var requests = new List<Domain.Request>();
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);
        _dbContextMock.Setup(x => x.Requests.Add(It.IsAny<Domain.Request>())).Callback<Domain.Request>(r => requests.Add(r));

        var requestDate = DateTimeOffset.Parse("2025-10-06");
        var birthDate = DateOnly.Parse("1982-01-19");
        _dbContextMock.Setup(x => x.Patients).ReturnsDbSet(new List<Patient>());

        // Act
        var command = new CreateRequestCommand
        {
            RequestDate = requestDate,
            SampleTubeType = 1,
            Patient = new PatientDto
            {
                PatientId = "PID001",
                BirthDate = birthDate
            }
        };
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        var createdRequest = _dbContextMock.Object.Requests.FirstOrDefault();
        createdRequest.Should().NotBeNull();
        createdRequest.PatientAge.Should().Be(43);
    }

    [Fact]
    public async Task CreateRequest_When_PatientExists_AndPatientBirthDateHasBeenChanged_Should_SetPatientAge()
    {
        // Arrange
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));
        var requests = new List<Domain.Request>();
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);
        _dbContextMock.Setup(x => x.Requests.Add(It.IsAny<Domain.Request>())).Callback<Domain.Request>(r => requests.Add(r));

        var requestDate = DateTimeOffset.Parse("2025-10-06");
        var birthDate = DateOnly.Parse("1982-01-19");
        var patient = new Patient
        {
            PatientId = "PID001",
            BirthDate = birthDate
        };
        _dbContextMock.Setup(x => x.Patients).ReturnsDbSet(new List<Patient> { patient });

        // Act
        var command = new CreateRequestCommand
        {
            RequestDate = requestDate,
            SampleTubeType = 1,
            Patient = new PatientDto
            {
                PatientId = "PID001",
                BirthDate = birthDate.AddYears(1) // <-- Change birthdate to one year later
            }
        };
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        var createdRequest = _dbContextMock.Object.Requests.FirstOrDefault();
        createdRequest.Should().NotBeNull();
        createdRequest.PatientAge.Should().Be(42);
    }

    [Fact]
    public async Task CreateRequest_When_PatientBirthDateIsToday_Should_SetPatientAgeTo0()
    {
        // Arrange
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));
        var requests = new List<Domain.Request>();
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);
        _dbContextMock.Setup(x => x.Requests.Add(It.IsAny<Domain.Request>())).Callback<Domain.Request>(r => requests.Add(r));

        var requestDate = DateTimeOffset.Parse("2025-10-06");
        var todayBirthDate = DateOnly.FromDateTime(DateTime.Now);
        var patient = new Patient
        {
            PatientId = "PID001",
            BirthDate = todayBirthDate
        };
        _dbContextMock.Setup(x => x.Patients).ReturnsDbSet(new List<Patient> { patient });

        // Act
        var command = new CreateRequestCommand
        {
            RequestDate = requestDate,
            SampleTubeType = 1,
            Patient = new PatientDto
            {
                PatientId = "PID001",
                BirthDate = todayBirthDate
            }
        };
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        var createdRequest = _dbContextMock.Object.Requests.FirstOrDefault();
        createdRequest.Should().NotBeNull();
        createdRequest.PatientAge.Should().Be(0); // Patient age should be set to 0
    }

    [Fact]
    public async Task CreateRequest_When_SampleTubeTypeIsMissing_Should_ThrowValidationException()
    {
        // Arrange
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));

        var requests = new List<Domain.Request>();
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);
        _dbContextMock.Setup(x => x.Requests.Add(It.IsAny<Domain.Request>())).Callback<Domain.Request>(r => requests.Add(r));
        _dbContextMock.Setup(x => x.Patients).ReturnsDbSet(new List<Patient>());

        // Act
        var command = new CreateRequestCommand
        {
            SampleId = "S1",
            SampleTubeType = null
        };

        var action = async () => await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage("Sample tube type must be set.");
    }
}
