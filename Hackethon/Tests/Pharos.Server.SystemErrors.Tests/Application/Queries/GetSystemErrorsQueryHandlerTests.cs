using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Localization;
using Pharos.Server.SystemErrors.Application.Interfaces;
using Pharos.Server.SystemErrors.Application.Queries.GetSystemErrors;
using Pharos.Server.SystemErrors.Domain;
using Xunit;

namespace Pharos.Server.SystemErrors.Tests.Application.Queries;

public class GetSystemErrorsQueryHandlerTests
{
    private readonly Mock<ISystemErrorsDbContext> _dbContext;
    private readonly Mock<IInstrumentManager> _instrumentService;
    private readonly Mock<IStringLocalizer<Translations>> _localizer;

    public GetSystemErrorsQueryHandlerTests()
    {
        _dbContext = new Mock<ISystemErrorsDbContext>();
        _instrumentService = new Mock<IInstrumentManager>();
        _localizer = new Mock<IStringLocalizer<Translations>>();

        _localizer
            .Setup(x => x["InstrumentFilter_System"])
            .Returns(new LocalizedString("InstrumentFilter_System", "System"));
    }

    [Fact]
    public async Task Handle_WithIncludeSystemErrorsOnly_ReturnsOnlySystemErrors()
    {
        // Arrange
        SetupSystemErrorsData(
            CreateSystemError(1, null),
            CreateSystemError(2, "INST-001"),
            CreateSystemError(3, "INST-002"));

        var sut = CreateSut();
        var request = new GetSystemErrorsQuery
        {
            IncludeSystemErrors = true,
            Count = 50
        };

        // Act
        var result = await sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        result.Items.Select(x => x.Id).Should().BeEquivalentTo([1]);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithInstrumentOnlyFilter_ReturnsOnlyMatchingInstrumentErrors()
    {
        // Arrange
        SetupSystemErrorsData(
            CreateSystemError(1, null),
            CreateSystemError(2, "INST-001"),
            CreateSystemError(3, "INST-002"));

        var sut = CreateSut();
        var request = new GetSystemErrorsQuery
        {
            InstrumentNumber = ["INST-001"],
            Count = 50
        };

        // Act
        var result = await sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        result.Items.Select(x => x.Id).Should().BeEquivalentTo([2]);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithSystemAndInstrumentFilters_ReturnsSystemAndMatchingInstrumentErrors()
    {
        // Arrange
        SetupSystemErrorsData(
            CreateSystemError(1, null),
            CreateSystemError(2, "INST-001"),
            CreateSystemError(3, "INST-002"));

        var sut = CreateSut();
        var request = new GetSystemErrorsQuery
        {
            IncludeSystemErrors = true,
            InstrumentNumber = ["INST-002"],
            Count = 50
        };

        // Act
        var result = await sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        result.Items.Select(x => x.Id).Should().BeEquivalentTo([1, 3]);
        result.TotalCount.Should().Be(2);
    }

    private GetSystemErrorsQueryHandler CreateSut()
    {
        return new GetSystemErrorsQueryHandler(_dbContext.Object, _instrumentService.Object, _localizer.Object);
    }

    private void SetupSystemErrorsData(params SystemError[] systemErrors)
    {
        _dbContext.Setup(x => x.SystemErrors).ReturnsDbSet(systemErrors);
    }

    private static SystemError CreateSystemError(int id, string? serialNumber)
    {
        return new SystemError
        {
            Id = id,
            Code = 1000 + id,
            Classification = ErrorClassification.Error,
            SerialNumber = serialNumber,
            Timestamp = DateTimeOffset.UtcNow,
            Source = ErrorSource.Pharos
        };
    }
}
