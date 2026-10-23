using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Instrument.Application.Queries.GetProcessStatusSummary;

/// <summary>
/// Query to get process status summary for lock screen display.
/// </summary>
public sealed record GetProcessStatusSummaryQuery(int InstrumentId) : IRequest<ProcessStatusSummaryDto>;

internal sealed class GetProcessStatusSummaryQueryHandler(IInstrumentManager instrumentManager)
    : IRequestHandler<GetProcessStatusSummaryQuery, ProcessStatusSummaryDto>
{
    public Task<ProcessStatusSummaryDto> Handle(GetProcessStatusSummaryQuery request, CancellationToken cancellationToken)
    {
        var instrument = (Domain.Instrument)instrumentManager.GetInstrument(request.InstrumentId);

        if (instrument.ProcessStatus == null)
        {
            return Task.FromResult(new ProcessStatusSummaryDto
            {
                RemainingResults = 0,
                LastPipettingTime = DateTimeOffset.UnixEpoch,
                LastRemainingResultTime = DateTimeOffset.UnixEpoch
            });
        }

        var processStatus = instrument.ProcessStatus;
        var processStatusSummary = new ProcessStatusSummaryDto
        {
            RemainingResults = processStatus.RemainingResults,
            LastPipettingTime = processStatus.LastRemainingPipettingTime?.ToDateTimeOffset().ToLocalTime() ?? DateTimeOffset.UnixEpoch,
            LastRemainingResultTime = GetLastRemainingResultTime(instrument, processStatus)
        };

        return Task.FromResult(processStatusSummary);
    }

    private static DateTimeOffset GetLastRemainingResultTime(
        Domain.Instrument instrument,
        Tetris.Protocol.Pharos.ProcessStatus.ProcessStatus processStatus)
    {
        return instrument.State == InstrumentState.AssayFinishOrdered
            ? processStatus.LastRemainingStartTime?.ToDateTimeOffset().ToLocalTime() ?? DateTimeOffset.UnixEpoch
            : processStatus.LastRemainingResultTime?.ToDateTimeOffset().ToLocalTime() ?? DateTimeOffset.UnixEpoch;
    }
}


public sealed record ProcessStatusSummaryDto
{
    public int RemainingResults { get; set; }

    public DateTimeOffset LastPipettingTime { get; set; }

    public DateTimeOffset LastRemainingResultTime { get; set; }
}

