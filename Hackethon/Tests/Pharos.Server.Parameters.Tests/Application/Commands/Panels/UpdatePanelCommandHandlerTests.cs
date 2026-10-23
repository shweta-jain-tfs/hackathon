using FluentValidation;
using Moq.EntityFrameworkCore;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.Panels;

public class UpdatePanelCommandHandlerTests
{
    private readonly List<Panel> _panels = [];
    private readonly List<PanelTest> _panelTests = [];
    private readonly List<MethodTest> _methodTests = [];
    private readonly Mock<IParametersDbContext> _mockContext;
    private readonly UpdatePanelCommandHandler _handler;

    public UpdatePanelCommandHandlerTests()
    {
        _mockContext = new Mock<IParametersDbContext>();
        _mockContext.Setup(x => x.Panels).ReturnsDbSet(_panels);
        _mockContext.Setup(x => x.PanelTests).ReturnsDbSet(_panelTests);
        _mockContext.Setup(x => x.MethodTests).ReturnsDbSet(_methodTests);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        Mock.Get(_mockContext.Object.PanelTests)
            .Setup(x => x.Remove(It.IsAny<PanelTest>()))
            .Callback<PanelTest>(panelTest => _panelTests.Remove(panelTest));

        _handler = new UpdatePanelCommandHandler(_mockContext.Object);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_ValidRequest_UpdatesPanelAndSynchronizesTests()
    {
        // Arrange
        var method = CreatePanelCommandHandlerTests.CreateMethod("sIgE");
        var existingMethodTest = CreatePanelCommandHandlerTests.CreateMethodTest(1, method, "c1", defaultReplicates: 2, defaultInstrumentDilutionFactor: 2);
        var addedMethodTest = CreatePanelCommandHandlerTests.CreateMethodTest(2, method, "d1", defaultReplicates: 1, defaultInstrumentDilutionFactor: 10);
        var deletedMethodTest = CreatePanelCommandHandlerTests.CreateMethodTest(3, method, "e1");

        var existingPanelTest = CreatePanelTest(100, existingMethodTest, replicates: 1, instrumentDilutionFactor: 1, sortOrder: 1);
        var deletedPanelTest = CreatePanelTest(101, deletedMethodTest, replicates: 1, instrumentDilutionFactor: 1, sortOrder: 2);
        var panel = new Panel
        {
            Id = 10,
            PanelName = "Old",
            PanelLongName = "Old panel",
            GroupedPanel = false,
            Enabled = false,
            Tests = [existingPanelTest, deletedPanelTest]
        };

        existingPanelTest.Panel = panel;
        deletedPanelTest.Panel = panel;
        _methodTests.AddRange([existingMethodTest, addedMethodTest, deletedMethodTest]);
        _panelTests.AddRange([existingPanelTest, deletedPanelTest]);
        _panels.Add(panel);

        var command = new UpdatePanelCommand
        {
            Id = panel.Id,
            PanelName = "New",
            PanelLongName = "New panel",
            Grouped = true,
            Enabled = true,
            Tests =
            [
                CreatePanelCommandHandlerTests.CreateTestCommand("sIgE", "d1", replicates: 3, instrumentDilutionFactor: 5, sortOrder: 1),
                new MethodPanelTestsDto { Id = 100, MethodName = "sIgE", TestName = "c1", Replicates = 0, SortOrder = 2 }
            ]
        };

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        panel.PanelName.Should().Be(command.PanelName);
        panel.PanelLongName.Should().Be(command.PanelLongName);
        panel.GroupedPanel.Should().BeTrue();
        panel.Enabled.Should().BeTrue();
        panel.Tests.Should().HaveCount(2);
        panel.Tests.OrderBy(x => x.SortOrder).Select(x => x.MethodTest.TestName).Should().Equal("d1", "c1");
        panel.Tests.OrderBy(x => x.SortOrder).Select(x => x.SortOrder).Should().Equal(1, 2);
        panel.Tests.Single(x => x.MethodTest.TestName == "c1").Replicates.Should().Be(existingMethodTest.Replicates);
        panel.Tests.Single(x => x.MethodTest.TestName == "d1").Replicates.Should().Be(3);
        panel.Tests.Single(x => x.MethodTest.TestName == "c1").InstrumentDilutionFactor.Should().Be(existingMethodTest.InstrumentDilutionFactor);
        panel.Tests.Single(x => x.MethodTest.TestName == "d1").InstrumentDilutionFactor.Should().Be(5);
        _panelTests.Should().NotContain(x => x.Id == 101);

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_PanelNotFound_ThrowsObjectGoneException()
    {
        // Arrange
        var command = new UpdatePanelCommand { Id = 42, PanelName = "Missing" };

        // Act & Assert
        await Assert.ThrowsAsync<ObjectGoneException>(() => _handler.Handle(command, TestContext.Current.CancellationToken));

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_DuplicatePanelName_ThrowsValidationException()
    {
        // Arrange
        _panels.AddRange(
        [
            new Panel { Id = 1, PanelName = "Existing", PanelLongName = "Existing", Tests = [] },
            new Panel { Id = 2, PanelName = "Changed", PanelLongName = "Changed", Tests = [] }
        ]);

        var command = new UpdatePanelCommand { Id = 2, PanelName = "Existing" };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, TestContext.Current.CancellationToken));

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_InvalidTest_ThrowsValidationException()
    {
        // Arrange
        _panels.Add(new Panel { Id = 1, PanelName = "Panel", PanelLongName = "Panel", Tests = [] });

        var command = new UpdatePanelCommand
        {
            Id = 1,
            PanelName = "Panel",
            Tests = [CreatePanelCommandHandlerTests.CreateTestCommand("sIgE", "missing")]
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, TestContext.Current.CancellationToken));

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_InvalidInstrumentDilutionFactor_ThrowsValidationException()
    {
        // Arrange
        var method = CreatePanelCommandHandlerTests.CreateMethod("sIgE");
        _methodTests.Add(CreatePanelCommandHandlerTests.CreateMethodTest(1, method, "c1"));
        _panels.Add(new Panel { Id = 1, PanelName = "Panel", PanelLongName = "Panel", Tests = [] });

        var command = new UpdatePanelCommand
        {
            Id = 1,
            PanelName = "Panel",
            Tests = [CreatePanelCommandHandlerTests.CreateTestCommand("sIgE", "c1", instrumentDilutionFactor: 3)]
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, TestContext.Current.CancellationToken));

        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static PanelTest CreatePanelTest(int id, MethodTest methodTest, int replicates, int instrumentDilutionFactor, int sortOrder)
    {
        return new PanelTest
        {
            Id = id,
            MethodTest = methodTest,
            MethodTestId = methodTest.Id,
            Replicates = replicates,
            InstrumentDilutionFactor = instrumentDilutionFactor,
            SortOrder = sortOrder
        };
    }
}
