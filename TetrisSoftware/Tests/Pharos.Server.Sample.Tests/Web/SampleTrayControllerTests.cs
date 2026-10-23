using MediatR;
using Pharos.Server.Sample.Application.Queries;
using Pharos.Server.Sample.Application.Queries.GetSampleTrayDetails;
using Pharos.Server.Sample.Web.Controllers;

namespace Pharos.Server.Sample.Tests.Web;

[Collection(nameof(SampleTestsCollection))]
public class SampleTrayControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly SampleTrayDetailsController _controller;
    public SampleTrayControllerTests()
    {
        _controller = new SampleTrayDetailsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetSampleTrayPositionsTest()
    {
        var trayId = "1";
        _mediatorMock.Setup(s => s.Send(It.IsAny<GetSampleTrayDetailsQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new SampleTrayDetailsDto() { SamplePositions = new List<SampleTrayPositionDto>() });

        var result = await _controller.GetSampleTrayDetails(trayId, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(result.SamplePositions);
        Assert.Empty(result.SamplePositions);
    }
}
