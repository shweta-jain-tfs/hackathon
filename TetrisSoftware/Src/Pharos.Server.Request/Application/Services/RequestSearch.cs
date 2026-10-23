using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Services;

internal class RequestSearch(IRequestDbContext dbContext, IAuthorizationService authorizationService, IOptionsSnapshot<RequestSettings> requestSettings) : ISearchProvider
{
    public async Task<List<SearchResultDto>> Search(string searchString, CancellationToken cancellationToken)
    {
        // Return empty results if search string is empty or whitespace-only
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return new List<SearchResultDto>();
        }

        var personalAndHealthPermissions = await authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);
        var showRequestorInformation = requestSettings.Value.IsRequestorInformationEnabled && personalAndHealthPermissions;
        var showPatientInformation = requestSettings.Value.IsPatientInformationEnabled && personalAndHealthPermissions;

        var results = new List<SearchResultDto>();

        // Filter requests in SQL only by non-encrypted fields
        var matchingRequests = await dbContext.Requests
            .Include(x => x.Patient)
            .Include(x => x.Requestor)
            .Where(x =>
                (x.RequestId != null && x.RequestId.Contains(searchString))
                || x.SampleId.Contains(searchString)
                || (showPatientInformation && x.Patient != null && x.Patient.PatientId != null && x.Patient.PatientId.Contains(searchString))
            )
            .OrderByDescending(x => x.Created)
            .ThenByDescending(x => x.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        // If permissions allow, search Requestors separately
        if (showRequestorInformation)
        {
            // Get all requestors first to be able to search for protected fields in memory
            var allRequestors = await dbContext.Requestors
                .ToListAsync(cancellationToken);

            // Find requestors that match the search criteria
            var matchingRequestors = allRequestors
                .Where(r => r.RequestorId.Contains(searchString, StringComparison.OrdinalIgnoreCase) || (r.Name != null && r.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(r => r.Created)
                .ThenByDescending(r => r.Id)
                .Take(50)
                .ToList();

            // For each matching requestor, create separate results for each field that matches
            foreach (var requestor in matchingRequestors)
            {
                // Check RequestorId
                if (requestor.RequestorId.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new SearchResultDto
                    {
                        Id = requestor.Id,
                        Match = requestor.RequestorId,
                        ResultType = SearchResultType.RequestorId,
                        DestinationHint = requestor.Name,
                        Module = SearchResultModules.Parameters
                    });
                }

                // Check Name
                if (requestor.Name != null && requestor.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new SearchResultDto
                    {
                        Id = requestor.Id,
                        Match = requestor.Name,
                        ResultType = SearchResultType.RequestorName,
                        DestinationHint = requestor.RequestorId,
                        Module = SearchResultModules.Parameters
                    });
                }
            }

            var matchingRequestorIds = matchingRequestors
                .Select(r => r.Id)
                .ToHashSet();

            if (matchingRequestorIds.Count > 0)
            {
                var requestMatchesFromRequestors = await dbContext.Requests
                    .Include(x => x.Patient)
                    .Include(x => x.Requestor)
                    .Where(x => x.RequestorId != null && matchingRequestorIds.Contains(x.RequestorId.Value))
                    .OrderByDescending(x => x.Created)
                    .ThenByDescending(x => x.Id)
                    .Take(50)
                    .ToListAsync(cancellationToken);

                matchingRequests = matchingRequests
                    .Concat(requestMatchesFromRequestors)
                    .DistinctBy(r => r.Id)
                    .ToList();
            }
        }

        // For each matching request, create separate results for each field that matches
        foreach (var request in matchingRequests)
        {
            // Check RequestId
            if (request.RequestId != null && request.RequestId.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new SearchResultDto
                {
                    Id = request.Id,
                    Match = request.RequestId,
                    ResultType = SearchResultType.RequestId,
                    Module = SearchResultModules.Request
                });
            }

            // Check SampleId
            if (request.SampleId.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new SearchResultDto
                {
                    Id = request.Id,
                    Match = request.SampleId,
                    ResultType = SearchResultType.SampleId,
                    Module = SearchResultModules.Request
                });
            }

            // Check RequestorId (if permissions allow)
            if (showRequestorInformation && request.Requestor != null && request.Requestor.RequestorId.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new SearchResultDto
                {
                    Id = request.Id,
                    Match = request.Requestor.RequestorId,
                    ResultType = SearchResultType.RequestorId,
                    DestinationHint = request.SampleId,
                    Module = SearchResultModules.Request
                });
            }

            // Check PatientId (if permissions allow)
            if (showPatientInformation && request.Patient != null && request.Patient.PatientId != null && request.Patient.PatientId.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new SearchResultDto
                {
                    Id = request.Id,
                    Match = request.Patient.PatientId,
                    ResultType = SearchResultType.PatientId,
                    DestinationHint = request.SampleId,
                    Module = SearchResultModules.Request
                });
            }
        }

        return results.ToList();
    }
}