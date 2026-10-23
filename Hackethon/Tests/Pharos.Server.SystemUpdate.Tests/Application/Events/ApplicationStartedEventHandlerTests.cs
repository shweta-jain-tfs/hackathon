using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.Application;
using Pharos.Server.SystemUpdate.Application.Events;
using Pharos.Server.SystemUpdate.Application.Interfaces;

namespace Pharos.Server.SystemUpdate.Tests.Application.Events;

public static class ApplicationStartedEventHandlerTests
{
    public class TheHandleMethod
    {
        private readonly ApplicationStartedEventHandler _sut;
        private readonly Mock<IUpdateOutcomeEvaluator> _updateChecker;

        public TheHandleMethod()
        {
            _updateChecker = new Mock<IUpdateOutcomeEvaluator>();

            _sut = new ApplicationStartedEventHandler(_updateChecker.Object, new Mock<ILogger<ApplicationStartedEventHandler>>().Object);
        }

        [Fact]
        public async Task CallsUpdateResultChecker_OnApplicationStarted()
        {
            // Act
            await _sut.Handle(new ApplicationStartedEvent(), TestContext.Current.CancellationToken);

            // Assert
            _updateChecker.Verify(c => c.EvaluateUpdateOutcome(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
