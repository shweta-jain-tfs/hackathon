using MediatR;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.Panels;

public class DeletePanelsCommandHandlerTests
{
    private readonly List<Panel> _panels = [];
    private readonly List<PanelTest> _panelTests = [];
    private readonly Mock<IParametersDbContext> _mockContext;
    private readonly Mock<ISender> _mockSender;
    private readonly DeletePanelsCommandHandler _handler;

    public DeletePanelsCommandHandlerTests()
    {
        _mockContext = new Mock<IParametersDbContext>();
        _mockSender = new Mock<ISender>();
        _mockContext.Setup(x => x.Panels).ReturnsDbSet(_panels);
        _mockContext.Setup(x => x.PanelTests).ReturnsDbSet(_panelTests);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockSender.Setup(x => x.Send(It.IsAny<RemoveTestPanelReferencesCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock.Get(_mockContext.Object.PanelTests)
            .Setup(x => x.Remove(It.IsAny<PanelTest>()))
            .Callback<PanelTest>(panelTest => _panelTests.Remove(panelTest));

        Mock.Get(_mockContext.Object.Panels)
            .Setup(x => x.Remove(It.IsAny<Panel>()))
            .Callback<Panel>(panel => _panels.Remove(panel));

        _handler = new DeletePanelsCommandHandler(_mockContext.Object, _mockSender.Object);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_WhenPanelsExist_DeletesPanelsAndRelatedTests()
    {
        // Arrange
        var firstPanel = CreatePanel(1, "First", CreatePanelTest(10), CreatePanelTest(11));
        var secondPanel = CreatePanel(2, "Second", CreatePanelTest(20));
        var remainingPanel = CreatePanel(3, "Remaining", CreatePanelTest(30));
        _panels.AddRange([firstPanel, secondPanel, remainingPanel]);
        _panelTests.AddRange(firstPanel.Tests.Concat(secondPanel.Tests).Concat(remainingPanel.Tests));

        // Act
        var result = await _handler.Handle(new DeletePanelsCommand([firstPanel.Id, secondPanel.Id], true), TestContext.Current.CancellationToken);

        // Assert
        result.Deleted.Should().Be(2);
        result.Skipped.Should().Be(0);
        result.SkippedIds.Should().BeEmpty();

        _panels.Should().ContainSingle(x => x.Id == remainingPanel.Id);
        _panelTests.Should().ContainSingle(x => x.Id == remainingPanel.Tests.Single().Id);
        _mockSender.Verify(x => x.Send(
            It.Is<RemoveTestPanelReferencesCommand>(command => command.PanelIds.OrderBy(id => id).SequenceEqual(new[] { firstPanel.Id, secondPanel.Id })),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_WhenSomeIdsDoNotExist_DeletesExistingPanelsAndReturnsSkippedIds()
    {
        // Arrange
        var panel = CreatePanel(1, "Existing", CreatePanelTest(10));
        _panels.Add(panel);
        _panelTests.AddRange(panel.Tests);

        // Act
        var result = await _handler.Handle(new DeletePanelsCommand([panel.Id, 99], true), TestContext.Current.CancellationToken);

        // Assert
        result.Deleted.Should().Be(1);
        result.Skipped.Should().Be(1);
        result.SkippedIds.Should().Equal(99);

        _panels.Should().BeEmpty();
        _panelTests.Should().BeEmpty();
        _mockSender.Verify(x => x.Send(
            It.Is<RemoveTestPanelReferencesCommand>(command => command.PanelIds.SequenceEqual(new[] { panel.Id })),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_WhenRemoveTestPanelReferencesIsFalse_DoesNotRemoveRequestReferences()
    {
        // Arrange
        var panel = CreatePanel(1, "Existing", CreatePanelTest(10));
        _panels.Add(panel);
        _panelTests.AddRange(panel.Tests);

        // Act
        var result = await _handler.Handle(new DeletePanelsCommand([panel.Id], false), TestContext.Current.CancellationToken);

        // Assert
        result.Deleted.Should().Be(1);
        _mockSender.Verify(x => x.Send(It.IsAny<RemoveTestPanelReferencesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_WhenNoIdsExist_ReturnsSkippedIdsAndDoesNotRemovePanels()
    {
        // Arrange
        var panel = CreatePanel(1, "Existing", CreatePanelTest(10));
        _panels.Add(panel);
        _panelTests.AddRange(panel.Tests);

        // Act
        var result = await _handler.Handle(new DeletePanelsCommand([99, 100], true), TestContext.Current.CancellationToken);

        // Assert
        result.Deleted.Should().Be(0);
        result.Skipped.Should().Be(2);
        result.SkippedIds.Should().Equal(99, 100);

        _panels.Should().ContainSingle(x => x.Id == panel.Id);
        _panelTests.Should().ContainSingle(x => x.Id == panel.Tests.Single().Id);
        _mockSender.Verify(x => x.Send(It.IsAny<RemoveTestPanelReferencesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_2589")]
    [Trait("ModuleRequirement", "MR_5972")]
    [Fact]
    public async Task Handle_WhenPanelDeleteSaveFails_DoesNotRemoveRequestReferences()
    {
        // Arrange
        var panel = CreatePanel(1, "Existing", CreatePanelTest(10));
        _panels.Add(panel);
        _panelTests.AddRange(panel.Tests);
        _mockContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Save failed."));

        // Act
        var act = async () => await _handler.Handle(new DeletePanelsCommand([panel.Id], true), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _mockSender.Verify(x => x.Send(It.IsAny<RemoveTestPanelReferencesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Panel CreatePanel(int id, string panelName, params PanelTest[] tests)
    {
        var panel = new Panel
        {
            Id = id,
            PanelName = panelName,
            PanelLongName = panelName,
            Tests = tests
        };

        foreach (var test in tests)
        {
            test.Panel = panel;
            test.PanelId = panel.Id;
        }

        return panel;
    }

    private static PanelTest CreatePanelTest(int id)
    {
        return new PanelTest
        {
            Id = id,
            Replicates = 1,
            InstrumentDilutionFactor = 1
        };
    }
}
