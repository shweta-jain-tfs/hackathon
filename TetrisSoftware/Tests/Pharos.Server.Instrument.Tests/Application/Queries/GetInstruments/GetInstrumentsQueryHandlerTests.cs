using System.Linq.Expressions;
using System.Net;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Instrument.Application.Queries.GetInstruments;

public class GetInstrumentsQueryHandlerTests
{
    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenActiveTechnologyHasNoEnabledMethods_ShouldExcludeItFromInstrumentCard()
    {
        // Arrange
        var instrument = new Domain.Instrument
        {
            Id = 1,
            SerialNumber = "SEED_001",
            IpAddress = IPAddress.Loopback,
            State = InstrumentState.Standby,
            ProcessStatus = new Tetris.Protocol.Pharos.ProcessStatus.ProcessStatus()
        };
        var instrumentManagerMock = new Mock<IInstrumentManager>();
        instrumentManagerMock.Setup(x => x.GetInstruments()).Returns([instrument]);
        var dbContextMock = new Mock<IInstrumentDbContext>();
        List<AssayScheduler> assaySchedulers = [];
        List<AssaySchedulerDay> assaySchedulerDays = [];
        dbContextMock.Setup(x => x.AssaySchedulers).ReturnsDbSet(assaySchedulers);
        dbContextMock.Setup(x => x.AssaySchedulerDays).ReturnsDbSet(assaySchedulerDays);
        List<InstrumentTechnology> instrumentTechnologies =
        [
            new() { InstrumentId = instrument.Id, Technology = AssayTechnology.ImmunoCap, IsActive = true },
            new() { InstrumentId = instrument.Id, Technology = AssayTechnology.EliA, IsActive = true }
        ];
        dbContextMock.Setup(x => x.InstrumentTechnologies).ReturnsDbSet(instrumentTechnologies);
        var methodsServiceMock = new Mock<IMethodsService>();
        methodsServiceMock
            .Setup(x => x.GetMethods(It.IsAny<Expression<Func<Method, bool>>>(), It.IsAny<Expression<Func<Method, AssayTechnology>>>()))
            .ReturnsAsync(new List<AssayTechnology> { AssayTechnology.ImmunoCap });
        var sut = new GetInstrumentsQueryHandler(instrumentManagerMock.Object, dbContextMock.Object, methodsServiceMock.Object);

        // Act
        var result = await sut.Handle(new GetInstrumentsQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().ContainSingle();
        result.Single().SelectedTechnologies.Should().Equal(AssayTechnology.ImmunoCap);
    }
}
