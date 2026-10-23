using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.AuditTrail;
using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.AuditTrail.Application.Interfaces;
using Pharos.Server.AuditTrail.Application.Queries;
using Pharos.Server.Localization;
using Pharos.Server.AuditTrail.Domain;

namespace Pharos.Server.AuditTrail.Queries.Filtering;

public class GetAuditTrailRecordsQueryHandlerFilteringTests
{
    private readonly Mock<IAuditTrailDbContext> _dbContext = new();
    private readonly Mock<DescriptionTemplateService> _descriptionService;

    public GetAuditTrailRecordsQueryHandlerFilteringTests()
    {
        var loc = new Mock<IStringLocalizer<Translations>>();
        loc.Setup(_ => _[It.IsAny<string>()])
           .Returns((string key) => new LocalizedString(key, key));
        _descriptionService = new Mock<DescriptionTemplateService>(loc.Object);
    }

    private GetAuditTrailRecordsQueryHandler CreateHandler()
    {
        return new GetAuditTrailRecordsQueryHandler(_dbContext.Object, _descriptionService.Object);
    }

    private void Seed(IEnumerable<AuditTrailRecord> records)
    {
        _dbContext.Setup(d => d.AuditTrailRecords).ReturnsDbSet(records);
    }

    private void SetupDescriptionTemplates()
    {
        _descriptionService
            .Setup(s => s.GetLocalizedDescription(EventId.UserLoggedIntoPharos, It.IsAny<string>()))
            .Returns("User logged into Pharos");
        _descriptionService
            .Setup(s => s.GetLocalizedDescription(EventId.UserLoggedOutPharos, It.IsAny<string>()))
            .Returns("User logged out of Pharos");
        _descriptionService
            .Setup(s => s.GetLocalizedDescription(EventId.UserAccountModified, It.IsAny<string>()))
            .Returns("User modified account");
    }

    private static List<AuditTrailRecord> MakeData(DateTimeOffset now)
    {
        return new List<AuditTrailRecord>()
        {
            new()
            {
                Id = 1,
                CreatedBy = "PharosUser1",
                Created = now.AddHours(-3),
                Comment = "log a",
                EventDataString = "",
                EventData = new List<AuditEventData>()
            },
            new()
            {
                Id = 2,
                CreatedBy = "PharosUser2",
                Created = now.AddHours(-2),
                Comment = "log b contains",
                EventDataString = "",
                EventData = new List<AuditEventData>()
            },
            new()
            {
                Id = 3,
                CreatedBy = "PharosUser3",
                Created = now.AddHours(-1),
                Comment = "log c",
                EventDataString = "contains",
                EventData = new List<AuditEventData>()
            },
            new()
            {
                Id = 4,
                CreatedBy = null,
                Created = now.AddHours(-4),
                Comment = "null user",
                EventDataString = "",
                EventData = new List<AuditEventData>()
            },
            new()
            {
                Id = 5,
                CreatedBy = "pharosuser",
                Created = now.AddHours(-4),
                Comment = "pharosuser",
                EventDataString = "",
                EventData = new List<AuditEventData>()
            },
            new()
            {
                Id = 6,
                CreatedBy = "PharosUser",
                Created = now.AddHours(-4),
                Comment = "PharosUser",
                EventDataString = "",
                EventData = new List<AuditEventData>()
            },
        };
    }

    [Trait("ProductRequirement", "PR_521")]
    [Trait("ModuleRequirement", "MR_5447")]
    [Fact]
    public async Task Filters_by_any_of_multiple_operators()
    {
        var now = DateTimeOffset.UtcNow;
        Seed(MakeData(now));
        var handler = CreateHandler();

        var query = new GetAuditTrailRecordsQuery
        {
            UserNames = new[] { "PharosUser1", "PharosUser2" },
            StartIndex = 0,
            Count = 50
        };

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Select(i => i.CreatedBy).Should().BeEquivalentTo(new[] { "PharosUser1", "PharosUser2" }, opts => opts.WithoutStrictOrdering());
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Filtering_ignores_null_CreatedBy_without_exception()
    {
        var now = DateTimeOffset.UtcNow;
        Seed(MakeData(now));
        var handler = CreateHandler();

        var query = new GetAuditTrailRecordsQuery
        {
            UserNames = new[] { "PharosUser1" },
            StartIndex = 0,
            Count = 50
        };

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Should().OnlyContain(i => i.CreatedBy == "PharosUser1");
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Composes_with_date_range_inclusive_ToDate()
    {
        var today = DateTimeOffset.UtcNow.Date;
        var data = new List<AuditTrailRecord>
        {
            new() { Id = 1, CreatedBy = "PharosUser1", Created = today.AddDays(-2), Comment = "", EventDataString = "", EventData = new List<AuditEventData>() },
            new() { Id = 2, CreatedBy = "PharosUser1", Created = today.AddDays(-1), Comment = "", EventDataString = "", EventData = new List<AuditEventData>() },
            new() { Id = 3, CreatedBy = "PharosUser2",   Created = today,            Comment = "", EventDataString = "", EventData = new List<AuditEventData>() },
        };
        Seed(data);
        var handler = CreateHandler();

        var query = new GetAuditTrailRecordsQuery
        {
            UserNames = new[] { "PharosUser1", "PharosUser2" },
            FromDate = today.AddDays(-1),
            ToDate = today,
            StartIndex = 0,
            Count = 50
        };

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { 2, 3 }, opts => opts.WithoutStrictOrdering());
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Sorts_by_Created_desc_and_paginates()
    {
        var now = DateTimeOffset.UtcNow;
        Seed(MakeData(now));
        var handler = CreateHandler();

        var query = new GetAuditTrailRecordsQuery
        {
            UserNames = new[] { "PharosUser1", "PharosUser2", "PharosUser3" },
            StartIndex = 1,
            Count = 1
        };

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Should().HaveCount(1);
        result.Items.Single().CreatedBy.Should().Be("PharosUser2");
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task No_user_filter_when_UserNames_is_empty_returns_all_matching_other_filters()
    {
        var now = DateTimeOffset.UtcNow;
        Seed(MakeData(now));
        var handler = CreateHandler();

        var query = new GetAuditTrailRecordsQuery
        {
            UserNames = Array.Empty<string>(),
            StartIndex = 0,
            Count = 50
        };

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Select(i => i.CreatedBy).Should().BeEquivalentTo(
            new[] { "PharosUser1", "PharosUser2", "PharosUser3", string.Empty, "pharosuser", "PharosUser" },
            opts => opts.WithoutStrictOrdering());

        result.Items.Should().HaveCount(6);
        result.TotalCount.Should().Be(6);
    }

    [Fact]
    public async Task User_filter_is_case_sensitive()
    {
        var now = DateTimeOffset.UtcNow;
        Seed(MakeData(now));
        var handler = CreateHandler();

        var query = new GetAuditTrailRecordsQuery
        {
            UserNames = new[] { "PharosUser" },
            StartIndex = 0,
            Count = 50
        };

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Select(i => i.CreatedBy).Should().BeEquivalentTo(
            new[] { "PharosUser" }, opts => opts.WithoutStrictOrdering());
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchText_matches_in_Activity_only()
    {
        SetupDescriptionTemplates();
        var now = DateTimeOffset.UtcNow;

        Seed(new[]
        {
            new AuditTrailRecord
            {
                Id = 1,
                EventId = EventId.UserLoggedIntoPharos,
                Created = now,
                EventDataString = "",
                Comment = "",
                EventData = new List<AuditEventData>()
            },
            new AuditTrailRecord
            {
                Id = 2,
                EventId = EventId.UserLoggedOutPharos,
                Created = now,
                EventDataString = "",
                Comment = "",
                EventData = new List<AuditEventData>()
            },
        });

        var handler = CreateHandler();
        var query = new GetAuditTrailRecordsQuery { SearchText = "logged out", StartIndex = 0, Count = 50 };

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { 2 });
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchText_is_case_insensitive_in_Activity()
    {
        SetupDescriptionTemplates();
        var now = DateTimeOffset.UtcNow;

        Seed(new[]
        {
            new AuditTrailRecord
            {
                Id = 1,
                EventId = EventId.UserLoggedIntoPharos,
                Created = now,
                EventDataString = "",
                Comment = "",
                EventData = new List<AuditEventData>()
            }
        });

        var handler = CreateHandler();
        var query = new GetAuditTrailRecordsQuery { SearchText = "LOGGED INTO", StartIndex = 0, Count = 50 };

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Should().ContainSingle(r => r.Id == 1);
        result.TotalCount.Should().Be(1);
    }

    [Trait("ProductRequirement", "PR_521")]
    [Trait("ModuleRequirement", "MR_5447")]
    [Fact]
    public async Task SearchText_composes_with_Date_and_Operator_filters()
    {
        SetupDescriptionTemplates();
        var today = DateTimeOffset.UtcNow.Date;

        Seed(new[]
        {
            new AuditTrailRecord
            {
                Id = 1,
                CreatedBy = "PharosUser1",
                EventId = EventId.UserLoggedIntoPharos,
                Created = today.AddDays(-1),
                EventDataString = "",
                Comment = "",
                EventData = new List<AuditEventData>()
            },
            new AuditTrailRecord
            {
                Id = 2,
                CreatedBy = "PharosUser2",
                EventId = EventId.UserLoggedIntoPharos,
                Created = today,
                EventDataString = "",
                Comment = "",
                EventData = new List<AuditEventData>()
            },
            new AuditTrailRecord
            {
                Id = 3,
                CreatedBy = "PharosUser1",
                EventId = EventId.UserLoggedOutPharos,
                Created = today,
                EventDataString = "",
                Comment = "",
                EventData = new List<AuditEventData>()
            },
        });

        var handler = CreateHandler();
        var query = new GetAuditTrailRecordsQuery
        {
            SearchText = "logged into",
            UserNames = new[] { "PharosUser1" },
            FromDate = today.AddDays(-1),
            ToDate = today,
            StartIndex = 0,
            Count = 50
        };

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { 1 });
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchText_matches_Activity_or_Comment_or_EventDataString()
    {
        SetupDescriptionTemplates();
        var now = DateTimeOffset.UtcNow;

        Seed(new[]
        {
            new AuditTrailRecord
            {
                Id = 1,
                EventId = EventId.UserLoggedIntoPharos,
                Created = now,
                EventDataString = "contains X",
                Comment = "",
                EventData = new List<AuditEventData>()
            },
            new AuditTrailRecord
            {
                Id = 2,
                EventId = EventId.UserLoggedIntoPharos,
                Created = now,
                EventDataString = "",
                Comment = "contains X",
                EventData = new List<AuditEventData>()
            },
            new AuditTrailRecord
            {
                Id = 3,
                EventId = EventId.UserLoggedOutPharos,
                Created = now,
                EventDataString = "",
                Comment = "",
                EventData = new List<AuditEventData>()
            },
        });

        var handler = CreateHandler();
        var query = new GetAuditTrailRecordsQuery { SearchText = "contains x", StartIndex = 0, Count = 50 };

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { 1, 2 }, opts => opts.WithoutStrictOrdering());
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchText_paginates_after_in_memory_filtering()
    {
        SetupDescriptionTemplates();
        var now = DateTimeOffset.UtcNow;
        var data = Enumerable.Range(1, 10).Select(i => new AuditTrailRecord
        {
            Id = i,
            EventId = EventId.UserLoggedIntoPharos,
            Created = now.AddMinutes(i),
            EventDataString = $"User logged into Pharos #{i}",
            Comment = "",
            EventData = new List<AuditEventData>()
        }).ToList();
        Seed(data);

        var handler = CreateHandler();
        var query = new GetAuditTrailRecordsQuery { SearchText = "logged into", StartIndex = 2, Count = 3 };

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
    }
}
