using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Infrastructure;
using Pharos.Server.Request.Application.Mapping;
using Pharos.Server.Request.Application.Queries.GetRequestDetails;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;

namespace Pharos.Server.Request.Application.Queries;

public class GetRequestDetailsQueryHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock;
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly Mock<IMethodsService> _methodsServiceMock;
    private readonly Mock<IInstrumentManager> _instrumentServiceMock;
    private readonly Mock<ISender> _senderMock;
    private readonly Mock<ISampleIdFormatter> _formatterMock;

    public GetRequestDetailsQueryHandlerTests()
    {
        _dbContextMock = new Mock<IRequestDbContext>();
        _authorizationServiceMock = new Mock<IAuthorizationService>();
        _methodsServiceMock = new Mock<IMethodsService>();
        _instrumentServiceMock = new Mock<IInstrumentManager>();
        _senderMock = new Mock<ISender>();
        _formatterMock = new Mock<ISampleIdFormatter>();

        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(true);

        _authorizationServiceMock
            .Setup(x => x.IsCurrentUserInRoleAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _senderMock.Setup(x => x.Send(It.IsAny<GetSampleTestResultsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SampleTestResultDto>());

        _formatterMock.Setup(x => x.Format(It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
            .Returns((string sampleId, DateTimeOffset _) => sampleId);

        _formatterMock.Setup(x => x.Format(It.IsAny<RequestUid>()))
            .Returns((RequestUid requestUid) => requestUid.SampleId);
    }

    [Fact]
    public async Task GetRequestDetails_When_IsPatientInformationEnabled_False_Should_Not_Include_Patient()
    {
        // Arrange
        var requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsPatientInformationEnabled = false,
            IsRequestorInformationEnabled = true
        });

        var lastModified = new DateTimeOffset(2026, 9, 19, 14, 30, 0, TimeSpan.Zero);
        var testRequest = new Domain.Request
        {
            Id = 1,
            SampleId = "SAMPLE001",
            RequestId = "REQ001",
            PreDilutionFactor = 1,
            LastModified = lastModified,
            // When IsPatientInformationEnabled is false, EF Core won't include the Patient
            // so it will be null (not loaded from the database)
            Patient = null,
            Requestor = new Requestor
            {
                Id = 20,
                RequestorId = "DOC001",
                Name = "Dr. Smith"
            },
            TestOrders = new List<TestOrder>()
        };

        var requests = new List<Domain.Request> { testRequest };
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);

        var handler = new GetRequestDetailsQueryHandler(
            _authorizationServiceMock.Object,
            _dbContextMock.Object,
            new RequestMapper(new Mock<IInstrumentManager>().Object),
            _methodsServiceMock.Object,
            _instrumentServiceMock.Object,
            requestSettingsMock.Object,
            _senderMock.Object,
            _formatterMock.Object,
            new Mock<IDateTime>().Object);

        var query = new GetRequestDetailsQuery(1, "INST001");

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.LastModified.Should().Be(lastModified);
        result.Patient.Should().BeNull("Patient information should not be included when IsPatientInformationEnabled is false");
        result.Requestor.Should().NotBeNull("Requestor information should be included when IsRequestorInformationEnabled is true");
        result.Requestor!.RequestorId.Should().Be("DOC001");

    }

    [Fact]
    public async Task GetRequestDetails_When_IsRequestorInformationEnabled_False_Should_Not_Include_Requestor()
    {
        // Arrange
        var requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsPatientInformationEnabled = true,
            IsRequestorInformationEnabled = false
        });

        var testRequest = new Domain.Request
        {
            Id = 1,
            SampleId = "SAMPLE001",
            RequestId = "REQ001",
            PreDilutionFactor = 1,
            Patient = new Patient
            {
                Id = 10,
                PatientId = "PID001",
                Name = "John Doe"
            },
            // When IsRequestorInformationEnabled is false, EF Core won't include the Requestor
            // so it will be null (not loaded from the database)
            Requestor = null,
            TestOrders = new List<TestOrder>()
        };

        var requests = new List<Domain.Request> { testRequest };
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);

        var handler = new GetRequestDetailsQueryHandler(
            _authorizationServiceMock.Object,
            _dbContextMock.Object,
            new RequestMapper(new Mock<IInstrumentManager>().Object),
            _methodsServiceMock.Object,
            _instrumentServiceMock.Object,
            requestSettingsMock.Object,
            _senderMock.Object,
            _formatterMock.Object,
            new Mock<IDateTime>().Object);

        var query = new GetRequestDetailsQuery(1, "INST001");

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Patient.Should().NotBeNull("Patient information should be included when IsPatientInformationEnabled is true");
        result.Patient!.PatientId.Should().Be("PID001");
        result.Requestor.Should().BeNull("Requestor information should not be included when IsRequestorInformationEnabled is false");

    }

    [Fact]
    public async Task GetRequestDetails_When_Both_Settings_False_Should_Not_Include_Patient_Or_Requestor()
    {
        // Arrange
        var requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsPatientInformationEnabled = false,
            IsRequestorInformationEnabled = false
        });

        var testRequest = new Domain.Request
        {
            Id = 1,
            SampleId = "SAMPLE001",
            RequestId = "REQ001",
            PreDilutionFactor = 1,
            // When both settings are false, EF Core won't include Patient or Requestor
            // so they will be null (not loaded from the database)
            Patient = null,
            Requestor = null,
            TestOrders = new List<TestOrder>()
        };

        var requests = new List<Domain.Request> { testRequest };
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);

        var handler = new GetRequestDetailsQueryHandler(
            _authorizationServiceMock.Object,
            _dbContextMock.Object,
            new RequestMapper(new Mock<IInstrumentManager>().Object),
            _methodsServiceMock.Object,
            _instrumentServiceMock.Object,
            requestSettingsMock.Object,
            _senderMock.Object,
            _formatterMock.Object,
            new Mock<IDateTime>().Object);

        var query = new GetRequestDetailsQuery(1, "INST001");

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Patient.Should().BeNull("Patient information should not be included when IsPatientInformationEnabled is false");
        result.Requestor.Should().BeNull("Requestor information should not be included when IsRequestorInformationEnabled is false");
    }
}
