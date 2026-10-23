using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Queries.GetRequests;

public class GetRequestsQuery : SortedPartialQuery<RequestSummaryDto>
{
    // Filter can be SampleId, Requestor or Patient.
    [ProtectedData]
    public string? Filter { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public List<RequestState>? SelectedStatuses { get; set; }
    public bool OnHold { get; set; }
    public List<RequestIssueType>? RequestIssueTypes { get; set; }
    public bool BypassRowLimit { get; set; }
}

internal sealed class GetRequestsQueryHandler : PartialQueryHandlerBase<GetRequestsQuery, Domain.Request, RequestSummaryDto>, IRequestHandler<GetRequestsQuery, PartialQueryResult<RequestSummaryDto>>
{
    private readonly IRequestDbContext _context;
    private readonly IOptionsSnapshot<RequestSettings> _requestSettings;
    private readonly IAuthorizationService _authorizationService;
    private readonly ISampleIdFormatter _sampleIdFormatter;
    private readonly ISender _sender;
    private readonly IInstrumentManager _instrumentManager;

    // Store permission results for use in projection
    private bool _canHandlePatientInfo;
    private bool _canHandleRequestorInfo;
    
    public GetRequestsQueryHandler(IRequestDbContext context, IOptionsSnapshot<RequestSettings> requestSettings, IAuthorizationService authorizationService, ISampleIdFormatter sampleIdFormatter, ISender sender, IInstrumentManager instrumentManager)
    {
        _context = context;
        _requestSettings = requestSettings;
        _authorizationService = authorizationService;
        _sampleIdFormatter = sampleIdFormatter;
        _sender = sender;
        _instrumentManager = instrumentManager;
    }

    public async Task<PartialQueryResult<RequestSummaryDto>> Handle(GetRequestsQuery request, CancellationToken cancellationToken)
    {
        var personalAndHealthPermissions = await _authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);

        _canHandlePatientInfo =
            _requestSettings.Value.IsPatientInformationEnabled &&
            personalAndHealthPermissions;

        _canHandleRequestorInfo =
            _requestSettings.Value.IsRequestorInformationEnabled &&
            personalAndHealthPermissions;

        var query = _context.Requests.AsQueryable();

        if (_canHandlePatientInfo)
        {
            query = query.Include(x => x.Patient);
        }
        if (_canHandleRequestorInfo)
        {
            query = query.Include(x => x.Requestor);
        }
        query = query.Include(x => x.TestOrders);

        var result = await GetPartialQueryResultAsync(query, request, cancellationToken);
        return result;
    }

    protected override IQueryable<Domain.Request> AddFiltering(IQueryable<Domain.Request> query, GetRequestsQuery request)
    {
        if (request.Filter != null)
        {
            query = query.Where(x =>
                x.SampleId.Contains(request.Filter) ||
                (_canHandlePatientInfo && x.Patient != null && (x.Patient.Name!.Contains(request.Filter) || x.Patient.PatientId!.Contains(request.Filter) || x.Patient.PatientNumber!.Contains(request.Filter))) ||
                (_canHandleRequestorInfo && x.Requestor != null && (x.Requestor.Name!.Contains(request.Filter) || x.Requestor.RequestorId.Contains(request.Filter))));
        }

        if (request.FromDate != null)
        {
            query = query.Where(x => x.SampleCollectionDate!.Value.Date >= request.FromDate.Value.Date);
        }

        if (request.ToDate != null)
        {
            query = query.Where(x => x.SampleCollectionDate!.Value.Date < request.ToDate.Value.Date.AddDays(1));
        }

        if (request.SelectedStatuses is { Count: > 0 })
        {
            query = query.Where(x => request.SelectedStatuses.Contains(x.RequestState));
        }

        if (request.OnHold)
        {
            query = query.Where(x => x.TestOrders.Any(t => !t.IsActive));
        }

        if (request.RequestIssueTypes is { Count: > 0 })
        {
            query = query.Where(x => x.Issues.Any(i => request.RequestIssueTypes.Contains(i.IssueType)));
        }

        return query;
    }

    protected override IQueryable<Domain.Request> AddSorting(IQueryable<Domain.Request> query, string? sortProperty, bool ascending)
    {
        // Domain default (no explicit sort)
        if (string.IsNullOrEmpty(sortProperty))
        {
            return query.OrderByDescending(x => x.Created)
                        .ThenByDescending(x => x.SampleId)
                        .ThenByDescending(x => x.Id);
        }

        var ordered = sortProperty switch
        {
            nameof(RequestSummaryDto.SampleCollectionDate) => ascending
                ? query.OrderBy(x => x.SampleCollectionDate.HasValue
                                        ? x.SampleCollectionDate.Value.Date
                                        : DateTimeOffset.MinValue)
                       .ThenBy(x => x.Created)
                : query.OrderByDescending(x => x.SampleCollectionDate.HasValue
                                        ? x.SampleCollectionDate.Value.Date
                                        : DateTimeOffset.MinValue)
                       .ThenByDescending(x => x.Created),

            _ => ascending
                ? query.OrderBy(x => x.Created)
                : query.OrderByDescending(x => x.Created)
        };

        return ascending
            ? ordered.ThenBy(x => x.SampleId)
                     .ThenBy(x => x.Id)
            : ordered.ThenByDescending(x => x.SampleId)
                     .ThenByDescending(x => x.Id);
    }

    protected override IQueryable<RequestSummaryDto> ApplyProjection(IQueryable<Domain.Request> query)
    {
        return query.Select(s => new RequestSummaryDto
        {
            Id = s.Id,
            RequestUid = new RequestUid
            {
                SampleId = s.SampleId,
                RequestCreatedDate = s.Created
            },
            SampleId = s.SampleId,
            SampleIdDisplay = _sampleIdFormatter.Format(s.SampleId, s.Created),
            SampleCollectionDate = s.SampleCollectionDate,
            PatientId = _canHandlePatientInfo ? s.Patient!.PatientId : null,
            PatientName = _canHandlePatientInfo ? s.Patient!.Name : null,
            RequestorId = _canHandleRequestorInfo ? s.Requestor!.RequestorId : null,
            RequestorName = _canHandleRequestorInfo ? s.Requestor != null ? s.Requestor.Name : null : null,
            State = s.RequestState,
            Issues = ConstructIssues(s.Issues)
        });
    }

    private static List<RequestIssueDto> ConstructIssues(IList<RequestIssue> issues)
    {
        return issues
            .GroupBy(i => i.IssueType)
            .Select(g =>
            {
                var highest = g.MaxBy(i => i.Urgency)!;
                return new RequestIssueDto
                {
                    IssueType = highest.IssueType,
                    Urgency = highest.Urgency,
                    Key = highest.Key
                };
            }).ToList();
    }

    protected override async Task AttachExtraInformation(List<RequestSummaryDto> items, CancellationToken cancellationToken)
    {
        await EnrichWithInstrumentAndTubeInfo(items, cancellationToken);
    }

    /// <summary>
    /// Enriches each DTO with the resolved tube type name and the display names of all instruments
    /// the sample is currently loaded in. Exposed as internal so unit tests can invoke it directly
    /// without going through the EF paging pipeline.
    /// </summary>
    internal async Task EnrichWithInstrumentAndTubeInfo(List<RequestSummaryDto> items, CancellationToken cancellationToken)
    {
        var currentTubes = await _sender.Send(
            new GetRequestListSampleTubesQuery(items.Select(x => x.RequestUid).ToList()),
            cancellationToken);

        var tubesByRequest = currentTubes.ToDictionary(x => ToKey(x.RequestUid), x => x.Tubes);

        foreach (var dto in items)
        {
            // Handle tube type for request
            var tubes = tubesByRequest.GetValueOrDefault(ToKey(dto.RequestUid), []);

            var instrumentNames = tubes
                .Select(t => t.InstrumentNumber)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .Select(n => _instrumentManager.FindInstrument(n!)?.DisplayName ?? n)
                .ToList();

            if (tubes.Count > 1)
            {
                dto.LastTray = null;
                dto.LastTrayId = null;
                dto.LastTrayTubePosition = null;
                dto.HasMultipleTubes = true;
            }
            else
            {
                var lastTube = tubes
                    .Where(t => !string.IsNullOrWhiteSpace(t.TrayId) && t.TubePosition.HasValue)
                    .OrderByDescending(t => t.LoadTimestamp)
                    .FirstOrDefault();

                var trayCoordinate = lastTube?.TubePositionDisplayValue;

                dto.LastTray = lastTube == null || string.IsNullOrEmpty(trayCoordinate)
                    ? null
                    : $"{lastTube.TrayId} ({trayCoordinate})";
                dto.LastTrayId = lastTube?.TrayId;
                dto.LastTrayTubePosition = lastTube?.TubePosition;
                dto.HasMultipleTubes = false;
            }

            dto.InstrumentName = instrumentNames.Count > 0 ? string.Join(", ", instrumentNames) : null;
        }
    }

    private static (string SampleId, DateTimeOffset RequestCreatedDate) ToKey(RequestUid requestUid)
    {
        return (requestUid.SampleId, requestUid.RequestCreatedDate);
    }
}
