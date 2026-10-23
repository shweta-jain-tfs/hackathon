using MediatR;
using Microsoft.Extensions.Localization;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Localization;
using Pharos.Server.SystemErrors.Application.Interfaces;
using Pharos.Server.SystemErrors.Domain;

namespace Pharos.Server.SystemErrors.Application.Queries.GetSystemErrors;

public class GetSystemErrorsQuery : SortedPartialQuery<SystemErrorSummaryDto>
{
    public string? Code { get; set; }
    public bool? UnhandledOnly { get; set; }
    public bool? IncludeSystemErrors { get; set; }
    public List<string>? InstrumentNumber { get; set; }
    public List<ErrorClassification>? Classification { get; set; }
}

internal class GetSystemErrorsQueryHandler : PartialQueryHandlerBase<GetSystemErrorsQuery, SystemError, SystemErrorSummaryDto>, IRequestHandler<GetSystemErrorsQuery, PartialQueryResult<SystemErrorSummaryDto>>
{
    private readonly ISystemErrorsDbContext _dbContext;
    private readonly IInstrumentManager _instrumentManager;
    private readonly IStringLocalizer<Translations> _loc;

    public GetSystemErrorsQueryHandler(ISystemErrorsDbContext dbContext, IInstrumentManager instrumentManager, IStringLocalizer<Translations> loc)
    {
        _dbContext = dbContext;
        _instrumentManager = instrumentManager;
        _loc = loc;
    }

    public async Task<PartialQueryResult<SystemErrorSummaryDto>> Handle(GetSystemErrorsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.SystemErrors;

        return await GetPartialQueryResultAsync(query, request, cancellationToken);
    }

    protected override IQueryable<SystemErrorSummaryDto> ApplyProjection(IQueryable<SystemError> query)
    {
        return query.Select(x => new SystemErrorSummaryDto
        {
            Id = x.Id,
            Code = x.Code,
            Classification = x.Classification,
            Timestamp = x.Timestamp,
            Response = x.Response,
            InstrumentSerialNumber = x.SerialNumber,
            IsAwaitingAcknowledge = !x.Response.HasValue && x.PossibleResponses.Contains(ResponseAction.Acknowledge),
            IsUnhandled = !x.Response.HasValue,
        });
    }

    protected override Task AttachExtraInformation(List<SystemErrorSummaryDto> items, CancellationToken cancellationToken)
    {
        var systemLabel = _loc["InstrumentFilter_System"].ToString();

        foreach (var instrumentErrorDto in items)
        {
            instrumentErrorDto.InstrumentName = _instrumentManager.FindInstrument(instrumentErrorDto.InstrumentSerialNumber)?.DisplayName
                                                ?? (instrumentErrorDto.InstrumentSerialNumber ?? systemLabel);
        }

        return Task.CompletedTask;
    }

    protected override IQueryable<SystemError> AddFiltering(IQueryable<SystemError> query, GetSystemErrorsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            if (int.TryParse(request.Code, out var code))
            {
                query = query.Where(x => x.Code == code);
            }
            else
            {
                query = query.Where(_ => false);
            }
        }

        if (request.UnhandledOnly == true)
        {
            query = query.Where(x => !x.Response.HasValue);
        }

        var includeSystemErrors = request.IncludeSystemErrors == true;
        var instrumentSerialNumbers = request.InstrumentNumber is { Count: > 0 }
            ? request.InstrumentNumber.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList()
            : [];

        if (includeSystemErrors || instrumentSerialNumbers.Count > 0)
        {
            query = query.Where(x => (includeSystemErrors && x.SerialNumber == null)
                                  || (instrumentSerialNumbers.Count > 0 && x.SerialNumber != null
                                  && instrumentSerialNumbers.Contains(x.SerialNumber)));
        }

        if (request.Classification is { Count: > 0 })
        {
            query = query.Where(x => request.Classification.Contains(x.Classification));
        }

        return query;
    }

    protected override IQueryable<SystemError> AddSorting(IQueryable<SystemError> query, string? sortProperty, bool ascending)
    {
        query = sortProperty switch
        {
            nameof(SystemErrorSummaryDto.Classification) => ascending ? query.OrderBy(x => x.Classification).ThenByDescending(x => x.Timestamp) : query.OrderByDescending(x => x.Classification).ThenByDescending(x => x.Timestamp),
            nameof(SystemErrorSummaryDto.Code) => ascending ? query.OrderBy(x => x.Code).ThenByDescending(x => x.Timestamp) : query.OrderByDescending(x => x.Code).ThenByDescending(x => x.Timestamp),
            nameof(SystemErrorSummaryDto.InstrumentName) => ascending ? query.OrderBy(x => x.SerialNumber).ThenByDescending(x => x.Timestamp) : query.OrderByDescending(x => x.SerialNumber).ThenByDescending(x => x.Timestamp),
            nameof(SystemErrorSummaryDto.Timestamp) => ascending ? query.OrderBy(x => x.Timestamp) : query.OrderByDescending(x => x.Timestamp),
            _ => query.OrderBy(x => x.Classification).ThenByDescending(x => x.Timestamp)
        };

        return query;
    }
}

