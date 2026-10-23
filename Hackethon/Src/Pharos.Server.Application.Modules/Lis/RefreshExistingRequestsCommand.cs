using MediatR;

namespace Pharos.Server.Application.Lis;

/// <summary>
/// Send this command to request test orders from LIS for samples that already have a request scanned by the instrument or manually via the external barcode reader
/// </summary>
/// <param name="SampleIDs">The list of samples IDs that do not have test orders.</param>
/// <param name="ManualScan">Indicates whether the request was initiated by a manual scan or not.</param>
public record RefreshExistingRequestsCommand(IReadOnlyCollection<string> SampleIDs, bool ManualScan) : IRequest;