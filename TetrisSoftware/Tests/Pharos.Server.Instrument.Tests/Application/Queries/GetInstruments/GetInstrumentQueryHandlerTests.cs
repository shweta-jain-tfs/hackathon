using System.Linq.Expressions;
using System.Net;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Instrument.Application.Queries.GetInstruments;

public class GetInstrumentQueryHandlerTests
{
    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenInstrumentExists_ShouldReturnActiveTechnologiesOrderedByTechnology()
    {
        // Arrange
        var instrument = CreateInstrument(1);
        var instrumentManagerMock = new Mock<IInstrumentManager>();
        instrumentManagerMock.Setup(x => x.GetInstrument(instrument.Id)).Returns(instrument);
        var dbContextMock = new Mock<IInstrumentDbContext>();
        var methodsServiceMock = CreateMethodsService(AssayTechnology.ImmunoCap);
        dbContextMock.Setup(x => x.InstrumentTechnologies).ReturnsDbSet(
        [
            CreateTechnology(instrument.Id, AssayTechnology.ViewAllergy, true),
            CreateTechnology(instrument.Id, AssayTechnology.ImmunoCap, true),
            CreateTechnology(instrument.Id, AssayTechnology.EliA, false),
            CreateTechnology(2, AssayTechnology.EliA, true)
        ]);
        var sut = new GetInstrumentQueryHandler(instrumentManagerMock.Object, dbContextMock.Object, methodsServiceMock.Object);

        // Act
        var result = await sut.Handle(new GetInstrumentQuery(instrument.Id), TestContext.Current.CancellationToken);

        // Assert
        result.SelectedTechnologies.Should().Equal(AssayTechnology.ImmunoCap);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenInstrumentDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var instrumentManagerMock = new Mock<IInstrumentManager>();
        instrumentManagerMock.Setup(x => x.GetInstrument(1)).Throws<KeyNotFoundException>();
        var sut = new GetInstrumentQueryHandler(
            instrumentManagerMock.Object,
            new Mock<IInstrumentDbContext>().Object,
            CreateMethodsService().Object);

        // Act
        var action = () => sut.Handle(new GetInstrumentQuery(1), TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
    }

    private static Domain.Instrument CreateInstrument(int id) =>
        new()
        {
            Id = id,
            SerialNumber = $"SEED_{id:000}",
            IpAddress = IPAddress.Loopback,
            State = InstrumentState.Standby,
            ProcessStatus = new Tetris.Protocol.Pharos.ProcessStatus.ProcessStatus()
        };

    private static InstrumentTechnology CreateTechnology(int instrumentId, AssayTechnology technology, bool isActive) =>
        new()
        {
            InstrumentId = instrumentId,
            Technology = technology,
            IsActive = isActive
        };

    private static Mock<IMethodsService> CreateMethodsService(params AssayTechnology[] enabledTechnologies)
    {
        var methodsServiceMock = new Mock<IMethodsService>();
        var methods = enabledTechnologies
            .Select(technology => new Method { Technology = technology, Enabled = true })
            .ToList();
        methodsServiceMock
            .Setup(x => x.GetMethods(It.IsAny<Expression<Func<Method, bool>>>(), It.IsAny<Expression<Func<Method, AssayTechnology>>>()))
            .ReturnsAsync((Expression<Func<Method, bool>> filter, Expression<Func<Method, AssayTechnology>> selector) =>
                methods.Where(filter.Compile()).Select(selector.Compile()).ToList());

        return methodsServiceMock;
    }
}
