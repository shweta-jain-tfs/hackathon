using Microsoft.Extensions.Options;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Request.Application.Queries.GetRequestor;
using Pharos.Server.Request.Application.Queries.GetRequestorDetails;

namespace Pharos.Server.Request.Web.Controllers;

public class RequestorsControllerTests
{
    [Fact]
    public async Task GetRequestor_When_IsRequestorInformationEnabled_False_Should_Return_Default()
    {
        // Arrange
        var senderMock = new Mock<ISender>();
        var requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsRequestorInformationEnabled = false
        });

        var controller = new RequestorsController(senderMock.Object, requestSettingsMock.Object);

        var query = new GetRequestorQuery
        {
            RequestorId = "DOC001"
        };

        // Act
        var result = await controller.GetRequestor(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull("Should return empty result when IsRequestorInformationEnabled is false");
        result.Items.Should().BeEmpty("Should return empty items list");
        result.TotalCount.Should().Be(0);

        // Verify that sender was never called since the setting is disabled
        senderMock.Verify(
            x => x.Send(It.IsAny<GetRequestorQuery>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not query database when IsRequestorInformationEnabled is false");
    }

    [Fact]
    public async Task GetRequestor_When_IsRequestorInformationEnabled_True_Should_Query_And_Return_Requestors()
    {
        // Arrange
        var senderMock = new Mock<ISender>();
        var requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsRequestorInformationEnabled = true
        });

        var expectedRequestors = new PartialQueryResult<RequestorDto>(
            [
                new RequestorDto
                {
                    RequestorId = "DOC001",
                    Name = "Dr. Smith"
                }
            ], 1, 1);

        senderMock
            .Setup(x => x.Send<PartialQueryResult<RequestorDto>>(It.IsAny<GetRequestorQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRequestors);

        var controller = new RequestorsController(senderMock.Object, requestSettingsMock.Object);

        var query = new GetRequestorQuery
        {
            RequestorId = "DOC001"
        };

        // Act
        var result = await controller.GetRequestor(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1, "IsRequestorInformationEnabled is true");
        result.Items.First().RequestorId.Should().Be("DOC001");
        result.Items.First().Name.Should().Be("Dr. Smith");
        result.TotalCount.Should().Be(1);

        // Verify that sender was called
        senderMock.Verify(
            x => x.Send<PartialQueryResult<RequestorDto>>(It.IsAny<GetRequestorQuery>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Should query database when IsRequestorInformationEnabled is true");
    }

    [Fact]
    public async Task GetRequestorDetails_When_IsRequestorInformationEnabled_False_Should_Return_Default()
    {
        // Arrange
        var senderMock = new Mock<ISender>();
        var requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsRequestorInformationEnabled = false
        });

        var controller = new RequestorsController(senderMock.Object, requestSettingsMock.Object);

        // Act
        var result = await controller.GetRequestorDetails(1, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull("IsRequestorInformationEnabled is false, so no requestor details should be returned");

        // Verify that sender was never called since the setting is disabled
        senderMock.Verify(
            x => x.Send(It.IsAny<GetRequestorDetailsQuery>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not query database when IsRequestorInformationEnabled is false");
    }

    [Fact]
    public async Task GetRequestorDetails_When_IsRequestorInformationEnabled_True_Should_Query_And_Return_Details()
    {
        // Arrange
        var senderMock = new Mock<ISender>();
        var requestSettingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        requestSettingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            IsRequestorInformationEnabled = true
        });

        var expectedDetails = new RequestorDetailsDto
        {
            Id = 1,
            RequestorId = "DOC001",
            Name = "Dr. Smith",
            Address = "123 Medical Drive",
            PhoneNumber = "555-1234"
        };

        senderMock
            .Setup(x => x.Send(It.IsAny<GetRequestorDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDetails);

        var controller = new RequestorsController(senderMock.Object, requestSettingsMock.Object);

        // Act
        var result = await controller.GetRequestorDetails(1, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.RequestorId.Should().Be("DOC001");
        result.Name.Should().Be("Dr. Smith");

        // Verify that sender was called
        senderMock.Verify(
            x => x.Send(It.IsAny<GetRequestorDetailsQuery>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Should query database when IsRequestorInformationEnabled is true");
    }
}
