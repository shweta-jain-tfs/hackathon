using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Request.Application.Queries.GetRequestId;

public class GetRequestIdQueryHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbMock;
    private readonly Mock<IDateTime> _dateTimeMock;
    private readonly Mock<IOptionsSnapshot<RequestSettings>> _settingsMock;
    private readonly Mock<IOptionsMonitor<SamplesSettings>> _samplesSettingsMock;

    public GetRequestIdQueryHandlerTests()
    {
        _dbMock = new Mock<IRequestDbContext>();
        _dateTimeMock = new Mock<IDateTime>();
        _settingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        _samplesSettingsMock = new Mock<IOptionsMonitor<SamplesSettings>>();
        _samplesSettingsMock.Setup(x => x.CurrentValue).Returns(new SamplesSettings());
    }

    private GetRequestIdQueryHandler GetSut()
    {
        return new GetRequestIdQueryHandler(_dbMock.Object, _dateTimeMock.Object, _settingsMock.Object, _samplesSettingsMock.Object);
    }

    [Fact]
    public async Task Handle_WithEmptyDatabase_ShouldReturnRequestDoesNotExist_NoMatterTheSetting()
    {
        // Arrange
        _dbMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request>());
        _dateTimeMock.Setup(x => x.Now).Returns(new DateTimeOffset(new DateTime(2025, 04, 01, 15, 20, 0), TimeSpan.Zero));
        _settingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            ReuseSampleId = false,
            DaysBeforeReuseOfSampleId = 0
        });

        // Act
        var sut = GetSut();
        var result = await sut.Handle(new GetRequestIdQuery("123456"), TestContext.Current.CancellationToken);

        // Assert
        result.RequestId.Should().Be(-1);
        result.RequestExists.Should().BeFalse();
        result.RequestCreatedDate.Should().Be(DateTimeOffset.MinValue);

        _settingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            ReuseSampleId = true,
            DaysBeforeReuseOfSampleId = 0
        });

        // Act
        var sut2 = GetSut();
        var result2 = await sut2.Handle(new GetRequestIdQuery("123456"), TestContext.Current.CancellationToken);

        // Assert
        result2.RequestId.Should().Be(-1);
        result2.RequestExists.Should().BeFalse();
        result2.RequestCreatedDate.Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task Handle_WithExistingRequestWithSameSampleId_ShouldReturnRequestExists_When_ReuseSampleIdIsFalse()
    {
        // Arrange
        var items = new List<Domain.Request>
        {
            new()
            {
                Id = 1,
                SampleId = "123456",
                Created = new DateTimeOffset(new DateTime(2025, 04, 01, 15, 20, 0), TimeSpan.Zero)
            }
        };
        _dbMock.Setup(x => x.Requests).ReturnsDbSet(items);
        _dateTimeMock.Setup(x => x.Now).Returns(new DateTimeOffset(new DateTime(2025, 04, 01, 15, 20, 0), TimeSpan.Zero));
        _settingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            ReuseSampleId = false,
            DaysBeforeReuseOfSampleId = 0
        });

        // Act
        var sut = GetSut();
        var result = await sut.Handle(new GetRequestIdQuery("123456"), TestContext.Current.CancellationToken);

        // Assert
        result.RequestId.Should().Be(1);
        result.RequestExists.Should().BeTrue();
        result.RequestCreatedDate.Should().Be(items[0].Created);
        result.SampleId.Should().Be(items[0].SampleId);
    }

    [Fact]
    public async Task Handle_WithExistingRequestWithSameSampleId_ShouldReturnRequestExists_When_ReuseSampleIdIsTrue_AndNotEnoughTimeHasPassed()
    {
        // Arrange
        var items = new List<Domain.Request>
        {
            new()
            {
                Id = 1,
                SampleId = "123456",
                Created = new DateTimeOffset(new DateTime(2025, 03, 31, 15, 20, 0), TimeSpan.Zero)
            }
        };
        _dbMock.Setup(x => x.Requests).ReturnsDbSet(items);
        _dateTimeMock.Setup(x => x.Now).Returns(new DateTimeOffset(new DateTime(2025, 04, 01, 15, 20, 0), TimeSpan.Zero));
        _settingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            ReuseSampleId = true,
            DaysBeforeReuseOfSampleId = 1
        });

        // Act
        var sut = GetSut();
        var result = await sut.Handle(new GetRequestIdQuery("123456"), TestContext.Current.CancellationToken);

        // Assert
        result.RequestId.Should().Be(1);
        result.RequestExists.Should().BeTrue();
        result.RequestCreatedDate.Should().Be(items[0].Created);
    }

    [Fact]
    public async Task Handle_WithExistingRequestWithSameSampleId_ShouldReturnRequestNotExists_When_ReuseSampleIdIsTrue_AndTimeHasPassed()
    {
        // Arrange
        var items = new List<Domain.Request>
        {
            new()
            {
                Id = 1,
                SampleId = "123456",
                Created = new DateTimeOffset(new DateTime(2025, 03, 30, 15, 20, 0), TimeSpan.Zero)
            }
        };
        _dbMock.Setup(x => x.Requests).ReturnsDbSet(items);
        _dateTimeMock.Setup(x => x.Now).Returns(new DateTimeOffset(new DateTime(2025, 04, 01, 15, 20, 0), TimeSpan.Zero));
        _settingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            ReuseSampleId = true,
            DaysBeforeReuseOfSampleId = 1
        });

        // Act
        var sut = GetSut();
        var result = await sut.Handle(new GetRequestIdQuery("123456"), TestContext.Current.CancellationToken);

        // Assert
        result.RequestId.Should().Be(-1);
        result.RequestExists.Should().BeFalse();
        result.RequestCreatedDate.Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task Handle_WithRequestCreatedDate_ShouldReturnRequestMatchingProvidedDate()
    {
        // Arrange
        var olderRequest = new Domain.Request
        {
            Id = 1,
            SampleId = "123456",
            Created = new DateTimeOffset(new DateTime(2025, 03, 29, 15, 20, 0), TimeSpan.Zero)
        };

        var newerRequest = new Domain.Request
        {
            Id = 2,
            SampleId = "123456",
            Created = new DateTimeOffset(new DateTime(2025, 04, 01, 10, 0, 0), TimeSpan.Zero)
        };

        _dbMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request>
        {
            olderRequest,
            newerRequest
        });

        _dateTimeMock.Setup(x => x.Now).Returns(new DateTimeOffset(new DateTime(2025, 04, 05, 12, 0, 0), TimeSpan.Zero));
        _settingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            ReuseSampleId = false,
            DaysBeforeReuseOfSampleId = 10
        });

        var requestCreatedDate = newerRequest.Created;

        // Act
        var sut = GetSut();
        var result = await sut.Handle(new GetRequestIdQuery("123456", requestCreatedDate), TestContext.Current.CancellationToken);

        // Assert
        result.RequestId.Should().Be(newerRequest.Id);
        result.RequestExists.Should().BeTrue();
        result.RequestCreatedDate.Should().Be(newerRequest.Created);
    }

    [Fact]
    public async Task Handle_WithDifferentlyCasedSampleId_ShouldReturnRequestExists_When_CaseInsensitiveSampleIdIsTrue()
    {
        // Arrange
        var items = new List<Domain.Request>
        {
            new()
            {
                Id = 1,
                SampleId = "AbC123",
                Created = new DateTimeOffset(new DateTime(2025, 04, 01, 15, 20, 0), TimeSpan.Zero)
            }
        };
        _dbMock.Setup(x => x.Requests).ReturnsDbSet(items);
        _dateTimeMock.Setup(x => x.Now).Returns(new DateTimeOffset(new DateTime(2025, 04, 01, 15, 20, 0), TimeSpan.Zero));
        _settingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            ReuseSampleId = false,
            DaysBeforeReuseOfSampleId = 0
        });
        _samplesSettingsMock.Setup(x => x.CurrentValue).Returns(new SamplesSettings
        {
            CaseInsensitiveSampleId = true
        });

        // Act
        var sut = GetSut();
        var result = await sut.Handle(new GetRequestIdQuery("abc123"), TestContext.Current.CancellationToken);

        // Assert
        result.RequestId.Should().Be(1);
        result.RequestExists.Should().BeTrue();
        result.RequestCreatedDate.Should().Be(items[0].Created);
        result.SampleId.Should().Be(items[0].SampleId);
    }

    [Fact]
    public async Task Handle_WithDifferentlyCasedSampleId_ShouldReturnRequestDoesNotExist_When_CaseInsensitiveSampleIdIsFalse()
    {
        // Arrange
        var items = new List<Domain.Request>
        {
            new()
            {
                Id = 1,
                SampleId = "AbC123",
                Created = new DateTimeOffset(new DateTime(2025, 04, 01, 15, 20, 0), TimeSpan.Zero)
            }
        };
        _dbMock.Setup(x => x.Requests).ReturnsDbSet(items);
        _dateTimeMock.Setup(x => x.Now).Returns(new DateTimeOffset(new DateTime(2025, 04, 01, 15, 20, 0), TimeSpan.Zero));
        _settingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            ReuseSampleId = false,
            DaysBeforeReuseOfSampleId = 0
        });
        _samplesSettingsMock.Setup(x => x.CurrentValue).Returns(new SamplesSettings
        {
            CaseInsensitiveSampleId = false
        });

        // Act
        var sut = GetSut();
        var result = await sut.Handle(new GetRequestIdQuery("abc123"), TestContext.Current.CancellationToken);

        // Assert
        result.RequestId.Should().Be(-1);
        result.RequestExists.Should().BeFalse();
        result.RequestCreatedDate.Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task Handle_WhenCaseSensitivitySettingChanges_ShouldUseCurrentValue()
    {
        var settings = new SamplesSettings { CaseInsensitiveSampleId = false };
        _samplesSettingsMock.Setup(x => x.CurrentValue).Returns(() => settings);
        _dbMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request>
        {
            new() { Id = 1, SampleId = "S1", Created = DateTimeOffset.Now }
        });
        _settingsMock.Setup(x => x.Value).Returns(new RequestSettings { ReuseSampleId = false });
        var sut = GetSut();

        var caseSensitiveResult = await sut.Handle(new GetRequestIdQuery("s1"), TestContext.Current.CancellationToken);
        settings = new SamplesSettings { CaseInsensitiveSampleId = true };
        var caseInsensitiveResult = await sut.Handle(new GetRequestIdQuery("s1"), TestContext.Current.CancellationToken);

        caseSensitiveResult.RequestExists.Should().BeFalse();
        caseInsensitiveResult.RequestExists.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithCaseInsensitiveSampleIdAndMultipleCasingVariants_ShouldReturnNewestRequest_WhenNoExactCaseExists()
    {
        // Arrange
        var olderRequest = new Domain.Request
        {
            Id = 1,
            SampleId = "ABC123",
            Created = new DateTimeOffset(new DateTime(2025, 04, 01, 15, 20, 0), TimeSpan.Zero)
        };
        var newerRequest = new Domain.Request
        {
            Id = 2,
            SampleId = "abc123",
            Created = new DateTimeOffset(new DateTime(2025, 04, 02, 15, 20, 0), TimeSpan.Zero)
        };

        _dbMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request> { olderRequest, newerRequest });
        _settingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            ReuseSampleId = false,
            DaysBeforeReuseOfSampleId = 0
        });
        _samplesSettingsMock.Setup(x => x.CurrentValue).Returns(new SamplesSettings
        {
            CaseInsensitiveSampleId = true
        });

        // Act
        var result = await GetSut().Handle(new GetRequestIdQuery("Abc123"), TestContext.Current.CancellationToken);

        // Assert
        result.RequestId.Should().Be(newerRequest.Id);
        result.RequestCreatedDate.Should().Be(newerRequest.Created);
    }

    [Fact]
    public async Task Handle_WithCaseInsensitiveSampleIdAndExactCaseMatch_ShouldPreferExactCase()
    {
        // Arrange
        var exactCaseRequest = new Domain.Request
        {
            Id = 1,
            SampleId = "Abc123",
            Created = new DateTimeOffset(new DateTime(2025, 04, 01, 15, 20, 0), TimeSpan.Zero)
        };
        var newerCasingVariant = new Domain.Request
        {
            Id = 2,
            SampleId = "ABC123",
            Created = new DateTimeOffset(new DateTime(2025, 04, 02, 15, 20, 0), TimeSpan.Zero)
        };

        _dbMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request> { exactCaseRequest, newerCasingVariant });
        _settingsMock.Setup(x => x.Value).Returns(new RequestSettings
        {
            ReuseSampleId = false,
            DaysBeforeReuseOfSampleId = 0
        });
        _samplesSettingsMock.Setup(x => x.CurrentValue).Returns(new SamplesSettings
        {
            CaseInsensitiveSampleId = true
        });

        // Act
        var result = await GetSut().Handle(new GetRequestIdQuery("Abc123"), TestContext.Current.CancellationToken);

        // Assert
        result.RequestId.Should().Be(exactCaseRequest.Id);
        result.RequestCreatedDate.Should().Be(exactCaseRequest.Created);
    }
}
