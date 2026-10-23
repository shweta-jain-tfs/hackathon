using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain.Enums;
using Tetris.Protocol.Pharos.ProcessStatus;

namespace Pharos.Server.Instrument.Application.Queries.GetInstrumentProcessList;

public class ProcessListItemDto
{
    public int SequenceNumber { get; set; }
    public string Identification { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Test { get; set; } = string.Empty;
    public DateTimeOffset? Time { get; set; }
    public ProcessingState State { get; set; }
    public int Id { get; set; } //TODO, remove id from this class. Now it is here to make the client happy and enable the use of VirtualGridViewModel
}

public class GetInstrumentProcessListQuery : SortedPartialQuery<ProcessListItemDto>
{
    public int InstrumentId { get; set; }
}

internal class GetInstrumentProcessListQueryHandler(
    IInstrumentManager intInstrumentManager) : IRequestHandler<GetInstrumentProcessListQuery, PartialQueryResult<ProcessListItemDto>>
{
    public Task<PartialQueryResult<ProcessListItemDto>> Handle(GetInstrumentProcessListQuery request, CancellationToken cancellationToken)
    {
        var instrument = (Domain.Instrument)intInstrumentManager.GetInstrument(request.InstrumentId);
        var totalCount = instrument.ProcessStatus?.ProcessListItems.Count ?? 0;
        var items = instrument.ProcessStatus?.ProcessListItems.Select(x => x.Test)
            .OrderBy(x => x.SequenceNumber)
            .Skip(request.StartIndex)
            .Take(request.Count);

        var processList = items?.Select(x => new ProcessListItemDto
        {
            SequenceNumber = x.SequenceNumber,
            Identification = x.SampleId,
            Method = x.MethodName,
            Test = x.TestName,
            Time = x.MeasureTime?.ToDateTimeOffset().ToLocalTime(),
            State = MapProcessingState(x.State),
            Id = 0
        }).ToList() ?? new List<ProcessListItemDto>();
        return Task.FromResult(new PartialQueryResult<ProcessListItemDto>(processList, request.StartIndex, totalCount));
    }

    /// <summary>
    /// Maps the processing state from the protocol to the domain.
    /// </summary>
    /// <param name="processingState"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private static ProcessingState MapProcessingState(EnumProcessingState processingState)
    {
        return processingState switch
        {
            EnumProcessingState.UnspecifiedProcessStep => ProcessingState.None,
            EnumProcessingState.UnableToStart => ProcessingState.UnableToStart,
            EnumProcessingState.Scheduled => ProcessingState.Scheduled,
            EnumProcessingState.Started => ProcessingState.Started,
            EnumProcessingState.Pipetted => ProcessingState.Pipetted,
            EnumProcessingState.Aborted => ProcessingState.Aborted,
            _ => throw new ArgumentOutOfRangeException(nameof(processingState), processingState, "Unknown processing state")
        };
    }
}