using FluentValidation;
using Moq.EntityFrameworkCore;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.Panels;

public class CreatePanelCommandHandlerTests
{
    private readonly List<Panel> _panels = [];
    private readonly List<PanelTest> _panelTests = [];
    private readonly List<MethodTest> _methodTests = [];
    private readonly Mock<IParametersDbContext> _mockContext;
    private readonly CreatePanelCommandHandler _handler;

    public CreatePanelCommandHandlerTests()
    {
        _mockContext = new Mock<IParametersDbContext>();
        _mockContext.Setup(x => x.Panels).ReturnsDbSet(_panels);
        _mockContext.Setup(x => x.PanelTests).ReturnsDbSet(_panelTests);
        _mockContext.Setup(x => x.MethodTests).ReturnsDbSet(_methodTests);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        Mock.Get(_mockContext.Object.Panels)
            .Setup(x => x.Add(It.IsAny<Panel>()))
            .Callback<Panel>(panel =>
            {
                panel.Id = 1;
                _panels.Add(panel);
                _panelTests.AddRange(panel.Tests);
            });

        _handler = new CreatePanelCommandHandler(_mockContext.Object);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_ValidRequest_CreatesPanelWithTestsInSortOrder()
    {
        // Arrange
        var method = CreateMethod("sIgE");
        var firstTest = CreateMethodTest(1, method, "c1", defaultReplicates: 2, defaultInstrumentDilutionFactor: 2);
        var secondTest = CreateMethodTest(2, method, "d1", defaultReplicates: 1);
        _methodTests.AddRange([firstTest, secondTest]);

        var command = new CreatePanelCommand
        {
            PanelName = "Allergy",
            PanelLongName = "Allergy panel",
            Grouped = true,
            Enabled = true,
            Tests =
            [
                CreateTestCommand("sIgE", "d1", replicates: 3, sortOrder: 2),
                CreateTestCommand("sIgE", "c1", replicates: 0, instrumentDilutionFactor: 5, sortOrder: 1)
            ]
        };

        // Act
        var panelId = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        panelId.Should().Be(1);
        _panels.Should().ContainSingle();

        var panel = _panels.Single();
        panel.PanelName.Should().Be(command.PanelName);
        panel.PanelLongName.Should().Be(command.PanelLongName);
        panel.GroupedPanel.Should().BeTrue();
        panel.Enabled.Should().BeTrue();
        panel.Tests.Should().HaveCount(2);
        panel.Tests.Select(x => x.MethodTest.TestName).Should().Equal("c1", "d1");
        panel.Tests.Select(x => x.SortOrder).Should().Equal(1, 2);
        panel.Tests.Select(x => x.Replicates).Should().Equal(firstTest.Replicates, 3);
        panel.Tests.Select(x => x.InstrumentDilutionFactor).Should().Equal(5, secondTest.InstrumentDilutionFactor);

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_WhenInstrumentDilutionFactorIsNotProvided_UsesMethodTestDefault()
    {
        // Arrange
        var method = CreateMethod("sIgE");
        var methodTest = CreateMethodTest(1, method, "c1", defaultInstrumentDilutionFactor: 10);
        _methodTests.Add(methodTest);

        var command = new CreatePanelCommand
        {
            PanelName = "Allergy",
            PanelLongName = "Allergy panel",
            Tests = [CreateTestCommand("sIgE", "c1", instrumentDilutionFactor: 0)]
        };

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _panels.Single().Tests.Single().InstrumentDilutionFactor.Should().Be(methodTest.InstrumentDilutionFactor);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_InvalidInstrumentDilutionFactor_ThrowsValidationException()
    {
        // Arrange
        var method = CreateMethod("sIgE");
        _methodTests.Add(CreateMethodTest(1, method, "c1"));

        var command = new CreatePanelCommand
        {
            PanelName = "Allergy",
            PanelLongName = "Allergy panel",
            Tests = [CreateTestCommand("sIgE", "c1", instrumentDilutionFactor: 3)]
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, TestContext.Current.CancellationToken));

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_InstrumentDilutionFactorBelowDefault_ThrowsValidationException()
    {
        // Arrange
        var method = CreateMethod("sIgE", preventDilutionFactorsBelowDefault: true);
        _methodTests.Add(CreateMethodTest(1, method, "c1", defaultInstrumentDilutionFactor: 10));

        var command = new CreatePanelCommand
        {
            PanelName = "Allergy",
            PanelLongName = "Allergy panel",
            Tests = [CreateTestCommand("sIgE", "c1", instrumentDilutionFactor: 5)]
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, TestContext.Current.CancellationToken));

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_DuplicatePanelName_ThrowsValidationException()
    {
        // Arrange
        _panels.Add(new Panel { PanelName = "Allergy", PanelLongName = "Existing", Tests = [] });

        var command = new CreatePanelCommand { PanelName = "Allergy", PanelLongName = "New" };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, TestContext.Current.CancellationToken));

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_DuplicateTests_ThrowsValidationException()
    {
        // Arrange
        var command = new CreatePanelCommand
        {
            PanelName = "Allergy",
            Tests =
            [
                CreateTestCommand("sIgE", "c1"),
                CreateTestCommand("sIgE", "c1")
            ]
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, TestContext.Current.CancellationToken));

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    internal static MethodPanelTestsDto CreateTestCommand(
        string methodName,
        string testName,
        int replicates = 1,
        int instrumentDilutionFactor = 0,
        int sortOrder = 1)
    {
        return new MethodPanelTestsDto
        {
            MethodName = methodName,
            TestName = testName,
            Replicates = replicates,
            InstrumentDilutionFactor = instrumentDilutionFactor,
            SortOrder = sortOrder
        };
    }

    internal static Method CreateMethod(string methodName, bool preventDilutionFactorsBelowDefault = false)
    {
        return new Method
        {
            Id = 1,
            MethodName = methodName,
            FullMethodName = methodName,
            PreventDilutionFactorsBelowDefault = preventDilutionFactorsBelowDefault,
            Tests = []
        };
    }

    internal static MethodTest CreateMethodTest(
        int id,
        Method method,
        string testName,
        int defaultReplicates = 1,
        int defaultInstrumentDilutionFactor = 1)
    {
        var methodTest = new MethodTest
        {
            Id = id,
            Method = method,
            MethodId = method.Id,
            TestName = testName,
            FullTestName = testName,
            DefaultLabTestName = testName,
            DefaultReplicates = defaultReplicates,
            DefaultDilutionMethod = DilutionMethod.InstrumentDilution,
            DefaultDilutionFactor = defaultInstrumentDilutionFactor
        };

        method.Tests.Add(methodTest);
        return methodTest;
    }
}
