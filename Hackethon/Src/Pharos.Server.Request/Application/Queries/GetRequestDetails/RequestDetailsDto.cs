using System.Text.Json.Serialization;
using Pharos.Server.Domain.Common;
using Pharos.Server.Request.Application.Queries.GetPatient;
using Pharos.Server.Request.Application.Queries.GetRequestor;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Queries.GetRequestDetails;

public class RequestDetailsDto
{
    public int Id { get; set; }
    public string SampleId { get; set; } = default!;
    public string SampleIdDisplay { get; set; } = default!;

    [JsonConverter(typeof(DateTimeOffsetToDateJsonConverter))]
    public DateTimeOffset? SampleCollectionDate { get; set; }
    public int PreDilutionFactor { get; set; }
    public string? RequestId { get; set; }

    [JsonConverter(typeof(DateTimeOffsetToDateJsonConverter))]
    public DateTimeOffset? RequestDate { get; set; }
    public bool HighPriority { get; set; }
    public int? PatientAgeAtSampling { get; set; }
    public List<RequestTestDto>? TestOrders { get; set; }
    public int? SampleTubeType { get; set; }
    public RequestState? RequestState { get; set; }
    public SampleVolumeCheck SampleVolumeCheck { get; set; }
    public PatientDto? Patient { get; set; }
    public RequestorDto? Requestor { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// The date and time when the request details were queried. This is used to determine if the request details are stale during updates.
    /// </summary>
    public DateTimeOffset QueriedAt { get; set; }
}
