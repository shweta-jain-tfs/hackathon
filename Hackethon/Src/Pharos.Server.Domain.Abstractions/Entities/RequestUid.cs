using System.ComponentModel.DataAnnotations;

namespace Pharos.Server.Domain.Entities;

/// <summary>
/// A globally unique request identifier.
/// </summary>
/// <remarks>
/// The lab might re-use sample ID (when they have, for example, a limited set of barcodes for sample tubes).
/// In this case, we cannot rely on the <see cref="SampleId"/> as the only lookup key for a specific request.
/// That is why we need a combination of fields as a unique ID for a request.
/// </remarks>
public class RequestUid
{
    /// <summary>The sample identifier received from LIS or entered by the user manually.</summary>
    [MaxLength(Constants.SampleIdMaxLength)]
    public string SampleId { get; set; } = default!;

    /// <summary>
    /// The timestamp of the moment when the request was created in the database.
    /// </summary>
    public DateTimeOffset RequestCreatedDate { get; set; }
}
