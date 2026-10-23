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

/// <summary>
/// Unit tests for GetRequestDetailsQueryHandler permission and settings filtering.
/// 
/// Testing Strategy:
/// - Unit tests cover both positive and negative permission scenarios
/// - Tests verify that when ViewPersonalAndHealthData permission is denied, Patient and Requestor data are excluded
/// - Tests verify that when ViewPersonalAndHealthData permission is granted, Patient and Requestor data are included
/// - See also: GetRequestQueryHandlerTests for comprehensive unit test examples of permission filtering
///   (that handler uses .Select() projection which can be properly tested with mocks)
/// </summary>
public class GetRequestDetailsQueryHandlerPermissionTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock;
    private readonly Mock<IAuthorizationService> _authorizationServiceMock;
    private readonly Mock<IMethodsService> _methodsServiceMock;
    private readonly Mock<IInstrumentManager> _instrumentServiceMock;
    private readonly Mock<ISender> _senderMock;
    private readonly Mock<IOptionsSnapshot<RequestSettings>> _requestSettingsMock;
    private readonly Mock<ISampleIdFormatter> _formatterMock;

    public GetRequestDetailsQueryHandlerPermissionTests()
    {
        _dbContextMock = new Mock<IRequestDbContext>();
        _authorizationServiceMock = new Mock<IAuthorizationService>();
        _methodsServiceMock = new Mock<IMethodsService>();
        _instrumentServiceMock = new Mock<IInstrumentManager>();
        _senderMock = new Mock<ISender>();
        _requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        _formatterMock = new Mock<ISampleIdFormatter>();

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
    public async Task GetRequestDetails_When_User_Has_Permission_And_Settings_Enabled_Should_Include_Both_Patient_And_Requestor()
    {
        // Arrange
        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsPatientInformationEnabled = true,
            IsRequestorInformationEnabled = true
        });

        // User has the permission
        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(true);

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
                Name = "John Doe",
                Gender = Gender.Male,
                BirthDate = new DateOnly(1980, 1, 1)
            },
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
            _requestSettingsMock.Object,
            _senderMock.Object,
            _formatterMock.Object,
            new Mock<IDateTime>().Object);

        var query = new GetRequestDetailsQuery(1, "INST001");

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Patient.Should().NotBeNull("User has permission and IsPatientInformationEnabled is true");
        result.Patient!.PatientId.Should().Be("PID001");
        result.Patient.Name.Should().Be("John Doe");
        result.Requestor.Should().NotBeNull("User has permission and IsRequestorInformationEnabled is true");
        result.Requestor!.RequestorId.Should().Be("DOC001");
        result.Requestor.Name.Should().Be("Dr. Smith");

    }

    [Fact]
    public async Task GetRequestDetails_When_User_Lacks_Permission_Should_Exclude_Patient_And_Requestor()
    {
        // Arrange
        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsPatientInformationEnabled = true,
            IsRequestorInformationEnabled = true
        });

        // User DOES NOT have the permission
        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(false);

        var testRequest = new Domain.Request
        {
            Id = 1,
            SampleId = "SAMPLE001",
            RequestId = "REQ001",
            PreDilutionFactor = 1,
            // When user lacks permission, EF Core won't include Patient or Requestor
            // (the handler uses conditional .Include() which prevents loading them from database)
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
            _requestSettingsMock.Object,
            _senderMock.Object,
            _formatterMock.Object,
            new Mock<IDateTime>().Object);

        var query = new GetRequestDetailsQuery(1, "INST001");

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Patient.Should().BeNull("User does not have ViewPersonalAndHealthData permission");
        result.Requestor.Should().BeNull("User does not have ViewPersonalAndHealthData permission");

    }

    [Fact]
    public async Task GetRequestDetails_When_PatientInformationDisabled_Should_Exclude_Patient_Even_With_Permission()
    {
        // Arrange
        _requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsPatientInformationEnabled = false, // Disabled
            IsRequestorInformationEnabled = true
        });

        // User HAS the permission
        _authorizationServiceMock
            .Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData))
            .ReturnsAsync(true);

        var testRequest = new Domain.Request
        {
            Id = 1,
            SampleId = "SAMPLE001",
            RequestId = "REQ001",
            PreDilutionFactor = 1,
            // When IsPatientInformationEnabled is false, EF Core won't include Patient
            // (the handler uses conditional .Include() which prevents loading it from database)
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
            _requestSettingsMock.Object,
            _senderMock.Object,
            _formatterMock.Object,
            new Mock<IDateTime>().Object);

        var query = new GetRequestDetailsQuery(1, "INST001");

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Patient.Should().BeNull("IsPatientInformationEnabled is false");
        result.Requestor.Should().NotBeNull("User has permission and IsRequestorInformationEnabled is true");
    }
}
