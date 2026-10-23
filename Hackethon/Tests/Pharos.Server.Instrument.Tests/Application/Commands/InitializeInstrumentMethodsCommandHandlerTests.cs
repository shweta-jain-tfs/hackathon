using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Instrument.Application.Commands.InitializeInstrumentMethods;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;

namespace Pharos.Server.Instrument.Application.Commands;

public class InitializeInstrumentMethodsCommandHandlerTests
{
    private readonly List<InstrumentTechnology> _instrumentTechnologies = [];
    private readonly List<InstrumentMethod> _instrumentMethods = [];
    private readonly List<AssayScheduler> _assaySchedulers = [];
    private readonly List<AssaySchedulerDay> _assaySchedulerDays = [];
    private readonly List<AssaySchedulerMethod> _assaySchedulerMethods = [];
    private readonly List<Method> _availableMethods = [];
    private readonly Mock<IInstrumentDbContext> _dbContextMock = new();
    private readonly Mock<IMethodsService> _methodsServiceMock = new();
    private readonly Mock<DbSet<InstrumentMethod>> _instrumentMethodsDbSetMock;
    private readonly InitializeInstrumentMethodsCommandHandler _sut;

    public InitializeInstrumentMethodsCommandHandlerTests()
    {
        _dbContextMock.Setup(x => x.InstrumentTechnologies).ReturnsDbSet(_instrumentTechnologies);
        _dbContextMock.Setup(x => x.InstrumentMethods).ReturnsDbSet(_instrumentMethods);
        _instrumentMethodsDbSetMock = Mock.Get(_dbContextMock.Object.InstrumentMethods);
        _dbContextMock.Setup(x => x.AssaySchedulers).ReturnsDbSet(_assaySchedulers);
        _dbContextMock.Setup(x => x.AssaySchedulerDays).ReturnsDbSet(_assaySchedulerDays);
        _dbContextMock.Setup(x => x.AssaySchedulerMethods).ReturnsDbSet(_assaySchedulerMethods);
        _methodsServiceMock
            .Setup(x => x.GetMethods(It.IsAny<Expression<Func<Method, bool>>>(), It.IsAny<Expression<Func<Method, string>>>()))
            .ReturnsAsync((Expression<Func<Method, bool>> filter, Expression<Func<Method, string>> selector) =>
                _availableMethods.Where(filter.Compile()).Select(selector.Compile()).ToList());

        _sut = new InitializeInstrumentMethodsCommandHandler(
            _dbContextMock.Object,
            _methodsServiceMock.Object,
            CreateConfiguration());
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenTechnologiesAreSelected_ShouldSynchronizeInstrumentMethods()
    {
        // Arrange
        _instrumentTechnologies.Add(new InstrumentTechnology
        {
            InstrumentId = 1,
            Technology = AssayTechnology.ImmunoCap,
            IsActive = true
        });
        _instrumentMethods.Add(new InstrumentMethod
        {
            InstrumentId = 1,
            MethodName = "EliA method",
            IsActive = true
        });
        _availableMethods.AddRange(
        [
            CreateMethod("ImmunoCAP method", AssayTechnology.ImmunoCap),
            CreateMethod("EliA method", AssayTechnology.EliA)
        ]);

        // Act
        await _sut.Handle(new InitializeInstrumentMethodsCommand(1), CancellationToken.None);

        // Assert
        _instrumentMethodsDbSetMock.Verify(
            x => x.RemoveRange(It.Is<IEnumerable<InstrumentMethod>>(methods =>
                methods.Count() == 1 && methods.Single().MethodName == "EliA method")),
            Times.Once);
        _instrumentMethodsDbSetMock.Verify(
            x => x.AddRangeAsync(
                It.Is<IEnumerable<InstrumentMethod>>(methods =>
                    methods.Count() == 1 && methods.Single().MethodName == "ImmunoCAP method"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenNoTechnologiesAreSelected_ShouldRemoveInstrumentMethods()
    {
        // Arrange
        _instrumentMethods.Add(new InstrumentMethod
        {
            InstrumentId = 1,
            MethodName = "ImmunoCAP method",
            IsActive = true
        });

        // Act
        await _sut.Handle(new InitializeInstrumentMethodsCommand(1), CancellationToken.None);

        // Assert
        _instrumentMethodsDbSetMock.Verify(
            x => x.RemoveRange(It.Is<IEnumerable<InstrumentMethod>>(methods =>
                methods.Count() == 1 && methods.Single().MethodName == "ImmunoCAP method")),
            Times.Once);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IConfiguration CreateConfiguration() =>
        CreateConfigurationMock(methodsActiveByDefault: false).Object;

    private static Mock<IConfiguration> CreateConfigurationMock(bool methodsActiveByDefault)
    {
        var configurationMock = new Mock<IConfiguration>();
        var instrumentSectionMock = new Mock<IConfigurationSection>();
        var methodsActiveByDefaultSectionMock = new Mock<IConfigurationSection>();

        methodsActiveByDefaultSectionMock
            .Setup(x => x.Value)
            .Returns(methodsActiveByDefault.ToString());
        instrumentSectionMock
            .Setup(x => x.GetSection("MethodsActiveByDefault"))
            .Returns(methodsActiveByDefaultSectionMock.Object);
        configurationMock
            .Setup(x => x.GetSection("Instrument"))
            .Returns(instrumentSectionMock.Object);

        return configurationMock;
    }

    private static Method CreateMethod(string methodName, AssayTechnology technology) =>
        new()
        {
            MethodName = methodName,
            Technology = technology,
            Enabled = true
        };
}
