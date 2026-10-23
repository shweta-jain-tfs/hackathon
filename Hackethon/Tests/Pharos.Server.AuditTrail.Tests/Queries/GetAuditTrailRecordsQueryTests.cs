using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application;
using Pharos.Server.Application.AuditTrail;
using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.Application.Queries;
using Pharos.Server.AuditTrail.Application.Interfaces;
using Pharos.Server.AuditTrail.Application.Queries;
using Pharos.Server.Localization;
using Pharos.Server.AuditTrail.Domain;

namespace Pharos.Server.AuditTrail.Queries;
public class GetAuditTrailRecordsQueryTests
{
    private readonly Mock<IAuditTrailDbContext> _dbContextMock;
    private readonly Mock<ICurrentUserService> _userService;
    private readonly Mock<DescriptionTemplateService> _descriptionTemplateService;

    public GetAuditTrailRecordsQueryTests()
    {
        _dbContextMock = new Mock<IAuditTrailDbContext>();
        var loc = new Mock<IStringLocalizer<Translations>>();
        SetupMockLocalizer(loc);
        _descriptionTemplateService = new Mock<DescriptionTemplateService>(loc.Object);
        _userService = new Mock<ICurrentUserService>();
    }

    private GetAuditTrailRecordsQueryHandler CreateHandler()
    {
        // The handler now expects dbContext to be initialized before _baseQuery is set.
        return new GetAuditTrailRecordsQueryHandler(_dbContextMock.Object, _descriptionTemplateService.Object);
    }

    [Fact]
    public async Task GetAuditTrailRecords_ShouldReturnAllRecords()
    {
        MockAuditTrailDbContext();
        var request = new GetAuditTrailRecordsQuery();

        var handler = CreateHandler();
        var result = await handler.Handle(request, TestContext.Current.CancellationToken);

        result.Should().NotBeNull().And.BeOfType<PartialQueryResult<AuditTrailRecordDto>>();
    }

    [Fact]
    public async Task GetAuditTrailRecords_WithSearchText_ShouldReturnFilteredResults()
    {
        MockAuditTrailDbContext();
        SetupDescriptionTemplateService();

        var request = new GetAuditTrailRecordsQuery
        {
            SearchText = "Write",
            StartIndex = 0,
            Count = 2
        };

        var handler = CreateHandler();
        var result = await handler.Handle(request, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Items.Should().HaveCountGreaterThanOrEqualTo(1);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetAuditTrailRecords_WithDateRange_ShouldReturnFilteredResults()
    {
        MockAuditTrailDbContext();
        SetupDescriptionTemplateService();

        var request = new GetAuditTrailRecordsQuery
        {
            SearchText = "Write",
            FromDate = DateTimeOffset.Now.AddDays(-2),
            ToDate = DateTimeOffset.Now,
            StartIndex = 0,
            Count = 10
        };

        var handler = CreateHandler();
        var result = await handler.Handle(request, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    private void MockAuditTrailDbContext()
    {
        _dbContextMock.Setup(db => db.AuditTrailRecords).ReturnsDbSet(GetAuditTrailRecords());
    }

    private List<AuditTrailRecord> GetAuditTrailRecords()
    {
        var now = DateTimeOffset.Now;
        var eventData = new AuditEventData();
        eventData.AddData(EventDataFields.UserId, _userService.Object.UserId);
        eventData.AddData(EventDataFields.Permissions, new List<string> { "Read", "Write" });

        return
        [
            new AuditTrailRecord
                {
                    Id = 1,
                    EventId = EventId.UserLoggedIntoPharos,
                    EventData = [eventData],
                    Comment = string.Empty,
                    Created = now.AddDays(-2),
                    EventDataString = StringHelper.GetEventDataString([eventData])
                },

                new AuditTrailRecord
                {
                    Id = 2,
                    EventId = EventId.UserAccountModified,
                    EventData = [eventData],
                    Created = now.AddDays(-2),
                    EventDataString = StringHelper.GetEventDataString([eventData])
                },

                new AuditTrailRecord
                {
                    Id = 3,
                    EventId = EventId.UserLoggedIntoPharos,
                    EventData = [new AuditEventData()],
                    Comment = string.Empty,
                    Created = now.AddDays(-1),
                    EventDataString = StringHelper.GetEventDataString([eventData])
                }
        ];
    }

    private void SetupDescriptionTemplateService()
    {
        _descriptionTemplateService.Setup(s =>
                s.GetLocalizedDescription(EventId.UserLoggedIntoPharos, It.IsAny<string>()))
            .Returns("User logged into Pharos");

        _descriptionTemplateService.Setup(s =>
                s.GetLocalizedDescription(EventId.UserAccountModified, It.IsAny<string>()))
            .Returns("User modified account");
    }

    private static void SetupMockLocalizer(Mock<IStringLocalizer<Translations>> mockLoc)
    {
        mockLoc.Setup(_ => _[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
    }
}

