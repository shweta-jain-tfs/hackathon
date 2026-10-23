using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Request.Application.Queries.GetRequestIdForSampleIds;

public class GetRequestIdForSampleIdsQueryHandlerTests
{
    [Fact]
    public async Task Handle_UsesResolvedRequestForExternalRequestId()
    {
        var requestCreatedDate = new DateTimeOffset(2025, 4, 1, 15, 20, 0, TimeSpan.Zero);
        var requests = new List<Domain.Request>
        {
            new()
            {
                Id = 2,
                SampleId = "S1",
                Created = requestCreatedDate,
                RequestId = "LIS-2"
            }
        };
        var dbContext = new Mock<IRequestDbContext>();
        dbContext.Setup(x => x.Requests).ReturnsDbSet(requests);

        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(
                new GetRequestIdQuery("s1", requestCreatedDate),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(2, requestCreatedDate, true, "S1"));

        var handler = new GetRequestIdForSampleIdsQueryHandler(dbContext.Object, sender.Object);
        var query = new GetRequestIdForSampleIdsQuery(
        [
            new RequestUid
            {
                SampleId = "s1",
                RequestCreatedDate = requestCreatedDate
            }
        ]);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result[("s1", requestCreatedDate)].Should().Be("LIS-2");
        sender.Verify(x => x.Send(
            new GetRequestIdQuery("s1", requestCreatedDate),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
