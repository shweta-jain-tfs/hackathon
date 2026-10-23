using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Pharos.Server.Application.AuditTrail;
using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.AuditTrail.Application.Interfaces;
using Pharos.Server.AuditTrail.Application.Queries;
using Pharos.Server.Localization;
using Pharos.Server.AuditTrail.Domain;
using EventId = Pharos.Server.Application.AuditTrail.Enums.EventId;

namespace Pharos.Server.AuditTrail.Queries;

public class GetAuditTrailExportQueryTests
{
    private readonly Mock<IAuditTrailDbContext> _dbContextMock = new();
    private readonly Mock<IStringLocalizer<Translations>> _localizerMock = new();
    private readonly GetAuditTrailExportQueryHandler _handler;

    public GetAuditTrailExportQueryTests()
    {
        _localizerMock
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        var loggerMock = new Mock<ILogger<GetAuditTrailExportQueryHandler>>();
        var descriptionService = new DescriptionTemplateService(_localizerMock.Object);
        _handler = new GetAuditTrailExportQueryHandler(_dbContextMock.Object, loggerMock.Object, descriptionService);
    }

    private void Seed(IEnumerable<AuditTrailRecord> records)
    {
        var list = records.ToList();
        _dbContextMock
            .Setup(db => db.AuditTrailRecords)
            .Returns(list.BuildMockDbSet().Object);
    }

    private static AuditTrailRecord MakeRecord(
        int id,
        EventId eventId,
        string createdBy,
        DateTimeOffset created,
        AuditEventData? eventData = null,
        string comment = "")
    {
        eventData ??= new AuditEventData();

        return new AuditTrailRecord
        {
            Id = id,
            EventId = eventId,
            EventData = [eventData],
            EventDataString = StringHelper.GetEventDataString([eventData]),
            Created = created,
            CreatedBy = createdBy,
            Comment = comment
        };
    }

    [Fact]
    public async Task Handle_ShouldAlwaysIncludeHeaderRow()
    {
        Seed([]);

        var result = await _handler.Handle(new GetAuditTrailExportQuery(), TestContext.Current.CancellationToken);

        result.Should().StartWith("Id;Date;Operator;Activity;Event Data;Comment");
    }

    [Fact]
    public async Task Handle_ShouldReturnOneDataRowPerRecord()
    {
        var now = DateTimeOffset.Now;
        Seed([
            MakeRecord(1, EventId.SampleModified, "user1", now),
            MakeRecord(2, EventId.SampleModified, "user2", now)
        ]);

        var result = await _handler.Handle(new GetAuditTrailExportQuery(), TestContext.Current.CancellationToken);

        // header + 2 data lines + trailing newline = 3 non-empty entries
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(3);
        result.Should().Contain("user1");
        result.Should().Contain("user2");
    }

    [Fact]
    public async Task Handle_ShouldIncludeAllCsvColumns_ForEachRecord()
    {
        var eventData = new AuditEventData();
        eventData.AddData(EventDataFields.Username, "test_user");

        Seed([new AuditTrailRecord
        {
            Id = 42,
            EventId = EventId.SampleModified,
            EventData = [eventData],
            EventDataString = StringHelper.GetEventDataString([eventData]),
            Created = DateTimeOffset.Now,
            CreatedBy = "admin_user",
            Comment = "Test comment"
        }]);

        var result = await _handler.Handle(new GetAuditTrailExportQuery(), TestContext.Current.CancellationToken);

        result.Should().Contain("42");
        result.Should().Contain(EventId.SampleModified.ToString());
        result.Should().Contain("test_user");
        result.Should().Contain("admin_user");
        result.Should().Contain("Test comment");
    }

    [Fact]
    public async Task Handle_ShouldIncludeRecords_OnOrAfterFromDate()
    {
        var now = DateTimeOffset.UtcNow;
        Seed([
            MakeRecord(1, EventId.SampleModified, "excluded", now.AddDays(-4)),
            MakeRecord(2, EventId.SampleModified, "included_boundary", now.AddDays(-3)),
            MakeRecord(3, EventId.SampleModified, "included_after", now)
        ]);

        var result = await _handler.Handle(
            new GetAuditTrailExportQuery { FromDate = now.AddDays(-3) },
            TestContext.Current.CancellationToken);

        result.Should().NotContain("excluded");
        result.Should().Contain("included_boundary");
        result.Should().Contain("included_after");
    }

    [Fact]
    public async Task Handle_ShouldIncludeRecords_OnOrBeforeToDate()
    {
        var now = DateTimeOffset.UtcNow;
        Seed([
            MakeRecord(1, EventId.SampleModified, "included", now.AddDays(-5)),
            MakeRecord(2, EventId.SampleModified, "included_boundary", now.AddDays(-2)),
            MakeRecord(3, EventId.SampleModified, "excluded_too_new", now)
        ]);

        var result = await _handler.Handle(
            new GetAuditTrailExportQuery { ToDate = now.AddDays(-2) },
            TestContext.Current.CancellationToken);

        result.Should().Contain("included");
        result.Should().Contain("included_boundary");
        result.Should().NotContain("excluded_too_new");
    }

    [Fact]
    public async Task Handle_ShouldFilterByDateRange_WhenBothDatesProvided()
    {
        var now = DateTimeOffset.UtcNow;
        Seed([
            MakeRecord(1, EventId.SampleModified, "excluded_too_old", now.AddDays(-10)),
            MakeRecord(2, EventId.SampleModified, "included_in_range", now.AddDays(-5)),
            MakeRecord(3, EventId.SampleModified, "excluded_too_new", now)
        ]);

        var result = await _handler.Handle(
            new GetAuditTrailExportQuery { FromDate = now.AddDays(-6), ToDate = now.AddDays(-3) },
            TestContext.Current.CancellationToken);

        result.Should().Contain("included_in_range");
        result.Should().NotContain("excluded_too_old");
        result.Should().NotContain("excluded_too_new");
    }

    [Fact]
    public async Task Handle_ShouldFilterByUserNames_WhenUserNamesProvided()
    {
        var now = DateTimeOffset.UtcNow;
        Seed([
            MakeRecord(1, EventId.SampleModified, "john.doe", now),
            MakeRecord(2, EventId.SampleModified, "jane.smith", now),
            MakeRecord(3, EventId.SampleModified, "admin", now)
        ]);

        var result = await _handler.Handle(
            new GetAuditTrailExportQuery { UserNames = ["john.doe", "jane.smith"] },
            TestContext.Current.CancellationToken);

        result.Should().Contain("john.doe");
        result.Should().Contain("jane.smith");
        result.Should().NotContain("admin");
    }

    [Fact]
    public async Task Handle_ShouldReturnAllRecords_WhenUserNamesIsEmpty()
    {
        var now = DateTimeOffset.UtcNow;
        Seed([
            MakeRecord(1, EventId.SampleModified, "user1", now),
            MakeRecord(2, EventId.SampleModified, "user2", now)
        ]);

        var result = await _handler.Handle(
            new GetAuditTrailExportQuery { UserNames = [] },
            TestContext.Current.CancellationToken);

        result.Should().Contain("user1");
        result.Should().Contain("user2");
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchText_MatchingComment()
    {
        Seed([
            MakeRecord(1, EventId.SampleModified, "user1", DateTimeOffset.UtcNow, comment: "changed dilution"),
            MakeRecord(2, EventId.SampleModified, "user2", DateTimeOffset.UtcNow, comment: "added new test"),
            MakeRecord(3, EventId.SampleModified, "user3", DateTimeOffset.UtcNow, comment: "tube type changed")
        ]);

        var result = await _handler.Handle(
            new GetAuditTrailExportQuery { SearchText = "changed" },
            TestContext.Current.CancellationToken);

        result.Should().Contain("user1");
        result.Should().Contain("user3");
        result.Should().NotContain("user2");
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchText_MatchingEventDataString()
    {
        var matchingEventData = new AuditEventData();
        matchingEventData.AddData(EventDataFields.Comments, "added test AAA-123");

        var otherEventData = new AuditEventData();
        otherEventData.AddData(EventDataFields.Comments, "added test BBB-321");

        Seed([
            MakeRecord(1, EventId.SampleModified, "user1", DateTimeOffset.UtcNow, matchingEventData),
            MakeRecord(2, EventId.SampleModified, "user2", DateTimeOffset.UtcNow, otherEventData)
        ]);

        var result = await _handler.Handle(
            new GetAuditTrailExportQuery { SearchText = "AAA-123" },
            TestContext.Current.CancellationToken);

        result.Should().Contain("user1");
        result.Should().NotContain("user2");
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchText_MatchingDescription()
    {
        _localizerMock
            .Setup(l => l["AuditTrail_SampleModified"])
            .Returns(new LocalizedString("AuditTrail_SampleModified", "Sample modified"));

        Seed([
            MakeRecord(1, EventId.TestAdded, "user1", DateTimeOffset.UtcNow),
            MakeRecord(2, EventId.SampleModified, "user2", DateTimeOffset.UtcNow)
        ]);

        var result = await _handler.Handle(
            new GetAuditTrailExportQuery { SearchText = "modified" },
            TestContext.Current.CancellationToken);

        result.Should().Contain("user2");
        result.Should().NotContain("user1");
    }

    [Fact]
    public async Task Handle_SearchText_ShouldBeCaseInsensitive()
    {
        Seed([
            MakeRecord(1, EventId.SampleModified, "user1", DateTimeOffset.UtcNow, comment: "UPPERCASE NOTE"),
            MakeRecord(2, EventId.SampleModified, "user2", DateTimeOffset.UtcNow, comment: "lowercase note"),
            MakeRecord(3, EventId.SampleModified, "user3", DateTimeOffset.UtcNow, comment: "MixedCase NoTe")
        ]);

        var result = await _handler.Handle(
            new GetAuditTrailExportQuery { SearchText = "note" },
            TestContext.Current.CancellationToken);

        result.Should().Contain("user1");
        result.Should().Contain("user2");
        result.Should().Contain("user3");
    }

    [Fact]
    public async Task Handle_ShouldReturnNoDataRows_WhenSearchTextMatchesNothing()
    {
        Seed([MakeRecord(1, EventId.SampleModified, "user1", DateTimeOffset.UtcNow, comment: "Modified sample data")]);

        var result = await _handler.Handle(
            new GetAuditTrailExportQuery { SearchText = "search with no match" },
            TestContext.Current.CancellationToken);

        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(1, "only the header row should remain");
        result.Should().NotContain("user1");
    }

    [Fact]
    public async Task Handle_ShouldApplyAllFilters_InCombination()
    {
        var now = DateTimeOffset.UtcNow;

        Seed([
            // Excluded: too old (before FromDate)
            MakeRecord(1, EventId.SampleModified, "john.doe", now.AddDays(-10), comment: "important note"),
            // Excluded: right date range and user, but comment doesn't match SearchText
            MakeRecord(2, EventId.SampleModified, "john.doe", now.AddDays(-2), comment: "routine check"),
            // Excluded: right date range and matching comment, but wrong user
            MakeRecord(3, EventId.SampleModified, "intruder", now.AddDays(-2), comment: "important note"),
            // Included: date in range, correct user, comment matches
            MakeRecord(4, EventId.SampleModified, "john.doe", now.AddDays(-2), comment: "important note")
        ]);

        var result = await _handler.Handle(new GetAuditTrailExportQuery
        {
            FromDate = now.AddDays(-3),
            ToDate = now,
            UserNames = ["john.doe"],
            SearchText = "important"
        }, TestContext.Current.CancellationToken);

        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2, "only the header and record 4 should survive all filters");
        result.Should().Contain("4;");
    }

    [Fact]
    public async Task Handle_ShouldEscapeDelimitersLineEndingsAndQuotes_InCsvFields()
    {
        var now = DateTimeOffset.UtcNow;
        var eventData = new AuditEventData();
        eventData.AddData(EventDataFields.Comments, "event;data\nevent \"value\"");

        _localizerMock
            .Setup(l => l["AuditTrail_SampleModified"])
            .Returns(new LocalizedString("AuditTrail_SampleModified", "activity;description\nactivity \"value\""));

        Seed([
            MakeRecord(1, EventId.SampleModified, "operator;name", now, eventData, "comment;value\ncomment \"value\"")
        ]);

        var result = await _handler.Handle(new GetAuditTrailExportQuery(), TestContext.Current.CancellationToken);

        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2);
        lines[1].Should().Be($"1;{now};\"operator;name\";\"activity;description | activity \"\"value\"\"\";\"Comments: event;data | event \"\"value\"\"\";\"comment;value | comment \"\"value\"\"\"");
    }

    [Theory]
    [InlineData("eventData1\neventData2")]
    [InlineData("eventData1\r\neventData2")]
    public async Task Handle_ShouldReplaceNewlines_InEventDataString_WithPipeSeparator(string newLine)
    {
        var eventData = new AuditEventData();
        eventData.AddData(EventDataFields.Comments, newLine);
        Seed([MakeRecord(1, EventId.SampleModified, "user1", DateTimeOffset.UtcNow, eventData)]);

        var result = await _handler.Handle(new GetAuditTrailExportQuery(), TestContext.Current.CancellationToken);

        result.Should().Contain("eventData1 | eventData2");
        result.Should().NotContain(newLine);
    }
}
