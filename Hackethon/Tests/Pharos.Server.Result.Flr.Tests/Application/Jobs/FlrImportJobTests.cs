using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Flr.Application.Commands;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace Pharos.Server.Result.Flr.Application.Jobs;

public class FlrImportJobTests : IAsyncLifetime
{
    private readonly Mock<ISender> _sender = new();
    private readonly Mock<ITempFileStorage> _tempFileStorage = new();
    private readonly Mock<IStreamReaderFactory> _streamReaderFactory = new();
    private readonly Mock<IJobHub> _jobHub = new();
    private readonly Mock<ILogger<FlrImportJob>> _logger = new();
    private readonly TaskCompletionSource _tcs = new();

    private static readonly Guid _userId = Guid.NewGuid();
    private static readonly Guid _fileId = Guid.NewGuid();
    private const string _fileName = "test.flr";
    private const string _filePath = "C:\\temp\\test.flr";

    private readonly StdSchedulerFactory _schedulerFactory;
    private readonly IScheduler _scheduler;

    public FlrImportJobTests()
    {
        // Use real in-memory scheduler
        _schedulerFactory = new StdSchedulerFactory();
        _scheduler = _schedulerFactory.GetScheduler().GetAwaiter().GetResult();
        _scheduler.JobFactory = new TestJobFactory(
            _sender.Object,
            _tempFileStorage.Object,
            _streamReaderFactory.Object,
            _jobHub.Object,
            _logger.Object
        );
        _jobHub.Setup(x => x.JobCompleted(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string>()))
            .Callback(() => _tcs.TrySetResult())
            .Returns(Task.CompletedTask);
    }

    public async ValueTask InitializeAsync()
    {
        await _scheduler.Start();
    }

    public async ValueTask DisposeAsync()
    {
        await _scheduler.Shutdown(waitForJobsToComplete: true);
    }

    [Fact]
    public async Task FlrImportJob_SuccessfulImport_DeletesFileAndNotifiesJobHub()
    {
        // Arrange
        var fileDeleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _tempFileStorage
            .Setup(x => x.DeleteFile(_fileId))
            .Callback(() => fileDeleted.TrySetResult());
        _tempFileStorage.Setup(x => x.GetFilePath(_fileId)).Returns(_filePath);
        _tempFileStorage.Setup(x => x.GetFileName(_fileId)).Returns(_fileName);
        _sender.Setup(x => x.Send(It.IsAny<ImportFlrFileCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _streamReaderFactory.Setup(x => x.Create(_filePath))
            .Returns(() => new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes("dummy FLR content"))));

        // Act: Schedule and trigger the job using the extension methods
        await _schedulerFactory.CreateFlrImportJob(_userId, _fileId);
        var inProgress = await _schedulerFactory.IsFlrImportJobInProgress();
        await _schedulerFactory.TriggerFlrImportJob();

        // Wait for the job to signal completion
        await _tcs.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await fileDeleted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // Assert
        inProgress.Should().BeTrue();
        _sender.Verify(x => x.Send(It.Is<ImportFlrFileCommand>(cmd => cmd.FileName == _fileName), It.IsAny<CancellationToken>()), Times.Once);
        _tempFileStorage.Verify(x => x.DeleteFile(_fileId), Times.Once);
        _jobHub.Verify(x => x.JobCompleted(JobNames.FlrImport, _userId, true, It.Is<string>(msg => msg.Contains(_fileName))), Times.Once);
    }

    [Fact]
    public async Task FlrImportJob_FileNotFound_ThrowsAndNotifiesJobHub()
    {
        // Arrange
        _tempFileStorage.Setup(x => x.GetFilePath(_fileId)).Returns((string?)null);

        // Act: Schedule and trigger the job using the extension methods
        await _schedulerFactory.CreateFlrImportJob(_userId, _fileId);
        await _schedulerFactory.TriggerFlrImportJob();

        // Wait for the job to signal completion
        await _tcs.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // Assert
        _jobHub.Verify(x => x.JobCompleted(JobNames.FlrImport, _userId, false, It.Is<string>(msg => msg.Contains("Failed", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
        _tempFileStorage.Verify(x => x.DeleteFile(It.IsAny<Guid>()), Times.Never);
        _sender.Verify(x => x.Send(It.IsAny<ImportFlrFileCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FlrImportJob_ImportThrowsException_DeletesFileAndNotifiesJobHub()
    {
        // Arrange
        _tempFileStorage.Setup(x => x.GetFilePath(_fileId)).Returns(_filePath);
        _tempFileStorage.Setup(x => x.GetFileName(_fileId)).Returns(_fileName);
        _sender.Setup(x => x.Send(It.IsAny<ImportFlrFileCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Import failed"));
        _streamReaderFactory.Setup(x => x.Create(_filePath))
            .Returns(() => new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes("dummy FLR content"))));

        // Act: Schedule and trigger the job using the extension methods
        await _schedulerFactory.CreateFlrImportJob(_userId, _fileId);
        await _schedulerFactory.TriggerFlrImportJob();

        // Wait for the job to signal completion
        await _tcs.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // Assert
        _tempFileStorage.Verify(x => x.DeleteFile(_fileId), Times.Once);
        _jobHub.Verify(x => x.JobCompleted(JobNames.FlrImport, _userId, false, It.Is<string>(msg => msg.Contains("Failed", StringComparison.InvariantCultureIgnoreCase))), Times.Once);
    }

    // Custom job factory to inject dependencies into FlrImportJob
    private class TestJobFactory(
        ISender sender,
        ITempFileStorage tempFileStorage,
        IStreamReaderFactory streamReaderFactory,
        IJobHub jobHub,
        ILogger<FlrImportJob> logger) : IJobFactory
    {
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            if (bundle.JobDetail.JobType == typeof(FlrImportJob))
            {
                return new FlrImportJob(sender, tempFileStorage, streamReaderFactory, jobHub, logger);
            }
            throw new NotSupportedException();
        }

        public void ReturnJob(IJob job) { }
    }
}