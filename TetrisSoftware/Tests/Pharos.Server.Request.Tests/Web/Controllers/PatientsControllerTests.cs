using Microsoft.Extensions.Options;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Request.Application.Queries.GetPatient;

namespace Pharos.Server.Request.Web.Controllers;

public class PatientsControllerTests
{
    [Fact]
    public async Task GetPatient_When_IsPatientInformationEnabled_False_Should_Return_EmptyResult()
    {
        // Arrange
        var senderMock = new Mock<ISender>();
        var requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsPatientInformationEnabled = false
        });

        var controller = new PatientsController(senderMock.Object, requestSettingsMock.Object);

        var query = new GetPatientQuery
        {
            PatientId = "PID001"
        };

        // Act
        var result = await controller.GetPatient(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty("IsPatientInformationEnabled is false, so no patient data should be returned");
        result.TotalCount.Should().Be(0);

        // Verify that sender was never called since the setting is disabled
        senderMock.Verify(
            x => x.Send(It.IsAny<GetPatientQuery>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not query database when IsPatientInformationEnabled is false");
    }

    [Fact]
    public async Task GetPatient_When_IsPatientInformationEnabled_True_Should_Query_And_Return_Patients()
    {
        // Arrange
        var senderMock = new Mock<ISender>();
        var requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsPatientInformationEnabled = true
        });

        var expectedPatients = new PartialQueryResult<PatientDto>(
            [
                new PatientDto
                {
                    PatientId = "PID001",
                    Name = "John Doe",
                    PatientNumber = "12345"
                }
            ], 1, 1);

        senderMock
            .Setup(x => x.Send<PartialQueryResult<PatientDto>>(It.IsAny<GetPatientQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPatients);

        var controller = new PatientsController(senderMock.Object, requestSettingsMock.Object);

        var query = new GetPatientQuery
        {
            PatientId = "PID001"
        };

        // Act
        var result = await controller.GetPatient(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1, "IsPatientInformationEnabled is true");
        result.Items.First().PatientId.Should().Be("PID001");
        result.Items.First().Name.Should().Be("John Doe");
        result.TotalCount.Should().Be(1);

        // Verify that sender was called
        senderMock.Verify(
            x => x.Send<PartialQueryResult<PatientDto>>(It.IsAny<GetPatientQuery>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Should query database when IsPatientInformationEnabled is true");
    }
}
