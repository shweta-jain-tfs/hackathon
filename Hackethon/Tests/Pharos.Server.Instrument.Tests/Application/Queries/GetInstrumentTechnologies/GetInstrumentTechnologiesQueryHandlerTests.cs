using System.Linq.Expressions;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Instrument.Application.Queries.GetInstrumentTechnologies;

public class GetInstrumentTechnologiesQueryHandlerTests
{
    private readonly List<Domain.Instrument> _instruments = [];
    private readonly List<InstrumentTechnology> _instrumentTechnologies = [];
    private readonly Mock<IInstrumentDbContext> _dbContextMock = new();
    private readonly Mock<IMethodsService> _methodsServiceMock = new();
    private readonly GetInstrumentTechnologiesQueryHandler _sut;
    private readonly List<Method> _availableMethods = [];

    public GetInstrumentTechnologiesQueryHandlerTests()
    {
        _dbContextMock.Setup(x => x.Instruments).ReturnsDbSet(_instruments);
        _dbContextMock.Setup(x => x.InstrumentTechnologies).ReturnsDbSet(_instrumentTechnologies);
        _methodsServiceMock
            .Setup(x => x.GetMethods(It.IsAny<Expression<Func<Method, bool>>>(), It.IsAny<Expression<Func<Method, AssayTechnology>>>()))
            .ReturnsAsync((Expression<Func<Method, bool>> filter, Expression<Func<Method, AssayTechnology>> selector) =>
                _availableMethods.Where(filter.Compile()).Select(selector.Compile()).ToList());
        _sut = new GetInstrumentTechnologiesQueryHandler(_dbContextMock.Object, _methodsServiceMock.Object);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenInstrumentExists_ShouldReturnItsTechnologiesOrderedByTechnology()
    {
        // Arrange
        _instruments.AddRange([CreateInstrument(1), CreateInstrument(2)]);
        _instrumentTechnologies.AddRange(
        [
            CreateTechnology(1, AssayTechnology.ViewAllergy, true),
            CreateTechnology(1, AssayTechnology.ImmunoCap, false),
            CreateTechnology(2, AssayTechnology.EliA, true)
        ]);
        _availableMethods.AddRange(
        [
            CreateMethod(AssayTechnology.ImmunoCap),
            CreateMethod(AssayTechnology.ViewAllergy)
        ]);

        // Act
        var result = await _sut.Handle(new GetInstrumentTechnologiesQuery(1), CancellationToken.None);

        // Assert
        result.Should().SatisfyRespectively(
            first =>
            {
                first.Technology.Should().Be(AssayTechnology.ImmunoCap);
                first.IsSelected.Should().BeFalse();
            },
            second =>
            {
                second.Technology.Should().Be(AssayTechnology.ViewAllergy);
                second.IsSelected.Should().BeTrue();
            });
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenTechnologyHasNoEnabledMethods_ShouldHideItUntilItBecomesAvailableAgain()
    {
        // Arrange
        _instruments.Add(CreateInstrument(1));
        _instrumentTechnologies.Add(CreateTechnology(1, AssayTechnology.EliA, false));
        _availableMethods.Add(CreateMethod(AssayTechnology.EliA, enabled: false));

        // Act
        var unavailableResult = await _sut.Handle(new GetInstrumentTechnologiesQuery(1), CancellationToken.None);

        // Assert
        unavailableResult.Should().BeEmpty();

        // Act
        _availableMethods.Add(CreateMethod(AssayTechnology.EliA));
        var availableResult = await _sut.Handle(new GetInstrumentTechnologiesQuery(1), CancellationToken.None);

        // Assert
        availableResult.Should().ContainSingle();
        availableResult.Single().Technology.Should().Be(AssayTechnology.EliA);
        availableResult.Single().IsSelected.Should().BeFalse();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenInstrumentDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange

        // Act
        var action = () => _sut.Handle(new GetInstrumentTechnologiesQuery(1), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    private static Domain.Instrument CreateInstrument(int id) =>
        new()
        {
            Id = id,
            SerialNumber = $"Instrument-{id}"
        };

    private static InstrumentTechnology CreateTechnology(int instrumentId, AssayTechnology technology, bool isActive) =>
        new()
        {
            InstrumentId = instrumentId,
            Technology = technology,
            IsActive = isActive
        };

    private static Method CreateMethod(AssayTechnology technology, bool enabled = true) =>
        new()
        {
            Technology = technology,
            Enabled = enabled
        };
}
