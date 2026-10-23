using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Lis;
using Pharos.Server.Lis.Application.Commands;
using Pharos.Server.Lis.Application.Commands.LisStatisticsCommands;
using Pharos.Server.Lis.Application.Queries;
using Pharos.Server.Lis.Application.Queries.GetLisChannels;
using Pharos.Server.Lis.Application.Queries.GetLisConnectionSummary;

namespace Pharos.Server.Lis.Web.Controllers;

[ApiController]
[Route("api/lis")]
public class LisController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Get all LIS connections with their current connection states.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    [Authorize]
    public async Task<IEnumerable<LisConnectionDto>> GetAll(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetLisQuery(), cancellationToken);
    }

    /// <summary>
    /// Gets all LIS channels from LisSettings
    /// </summary>
    [HttpGet("channels")]
    [Authorize]
    public async Task<IList<LisChannelDto>> GetChannels(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetLisChannelsQuery(), cancellationToken);
    }

    /// <summary>
    /// Get all LIS connections with their current connection states and summarized statistics for queries, imports, and exports.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("all/summary")]
    [Authorize]
    public async Task<List<LisConnectionSummaryDto>> GetAllSummary(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetAllLisConnectionSummaryQuery(), cancellationToken);
    }

    /// <summary>
    /// Retrieves a summary of the LIS connection for the specified channel.
    /// </summary>
    /// <remarks>This method requires the caller to be authorized.</remarks>
    /// <param name="channelId">The unique identifier of the channel for which to retrieve the LIS connection summary.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="LisConnectionSummaryDto"/> containing summary information about the LIS connection for the
    /// specified channel.</returns>
    [HttpGet("{channelId}/summary")]
    [Authorize]
    public async Task<LisConnectionSummaryDto> GetSummary(int channelId, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetLisConnectionSummaryQuery(channelId), cancellationToken);
    }

    /// <summary>
    /// Resets the LIS statistics for the specified channel.
    /// </summary>
    /// <param name="channelId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{channelId}/statistics")]
    [Authorize(Permissions.EditSystemSettings)]
    [Authorize(Permissions.ManageTestOrders)]
    public async Task ResetStatistics(int channelId, CancellationToken cancellationToken)
    {
        await sender.Send(new ResetLisStatisticsCommand(channelId), cancellationToken);
    }

    /// <summary>
    /// Sends a request to the LIS to query test orders for the specified sample IDs.
    /// This endpoint is invoked by clients to manually trigger test order queries for 'All' or 'One or More Selected Samples'.
    /// </summary>
    /// <param name="sampleIds">Array of sample identifiers to query test orders for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpPost("query-test-orders")]
    [Authorize(Permissions.ManageTestOrders)]
    public async Task QueryTestOrdersForSamples(string[] sampleIds, CancellationToken cancellationToken)
    {
        await sender.Send(new SendManualTestOrderRequestsToLisCommand(sampleIds), cancellationToken);
    }

    /// <summary>
    /// Queries the LIS to refresh existing test orders for a scanned sample barcode.
    /// This endpoint is invoked when a sample with an existing request is scanned via an external barcode scanner
    /// and the LIS setting "Refresh existing request (manual scan)" is enabled.
    /// </summary>
    /// <param name="sampleId">The sample identifier from the scanned barcode.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpPost("query-refresh-request")]
    [Authorize(Permissions.ManageTestOrders)]
    public async Task QueryRefreshExistingRequest(string sampleId, CancellationToken cancellationToken)
    {
        await sender.Send(new RefreshExistingRequestsCommand([sampleId], true), cancellationToken);
    }

    /// <summary>
    /// Queries the LIS to request test orders for a scanned sample barcode without an existing request.
    /// This endpoint is invoked when a sample without a linked request is scanned via an external barcode scanner
    /// and the LIS setting "Ask for missing request (manual scan)" is enabled.
    /// </summary>
    /// <param name="sampleId">The sample identifier from the scanned barcode.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpPost("query-ask-missing-request")]
    [Authorize(Permissions.ManageTestOrders)]
    public async Task QueryAskMissingRequest(string sampleId, CancellationToken cancellationToken)
    {
        await sender.Send(new AskForMissingTestOrdersCommand([sampleId], true), cancellationToken);
    }
}