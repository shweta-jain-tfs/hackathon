using MediatR;
using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Application.Result;

/// <summary>
/// This event is triggered when the status of one or more sample test results changes.
/// </summary>
/// <param name="TestResults">The list of test results that have changed the status.</param>
public record TestResultStatusChangedEvent(IReadOnlyList<ITestResult> TestResults) : INotification;

