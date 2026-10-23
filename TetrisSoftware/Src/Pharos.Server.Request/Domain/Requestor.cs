#pragma warning disable CS8618

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Request.Domain;

internal class Requestor : BaseAuditableEntity
{
    [MaxLength(Constants.RequestorIdLength)]
    [ProtectedData]
    public string RequestorId { get; set; }

    [MaxLength(Constants.NameLength)]
    [ProtectedData]
    public string? Name { get; set; } = string.Empty;

    [MaxLength(Constants.AddressLength)]
    [ProtectedData]
    public string? Address { get; set; } = string.Empty;

    [MaxLength(Constants.CommentLength)]
    [ProtectedData]
    public string? Comment { get; set; } = string.Empty;

    [MaxLength(Constants.ZipCodeLength)]
    [ProtectedData]
    public string? ZipCode { get; set; } = string.Empty;

    [MaxLength(Constants.OrderingPhysicianCodeLength)]
    [ProtectedData]
    public string? OrderingPhysicianCode { get; set; } = string.Empty;

    [MaxLength(Constants.PhoneNumberLength)]
    [ProtectedData]
    public string? PhoneNumber { get; set; } = string.Empty;

    public bool ReflexEnabled { get; set; } = true;

    [MaxLength(Constants.ShortcutCodeLength)]
    [ProtectedData]
    public string? ShortcutCode { get; set; } = string.Empty;

    public DateTimeOffset? LatestSampleDate { get; set; }

    public int NumberOfReportedSamples { get; set; }

    public int NumberOfReportedTests { get; set; }

    public DateTimeOffset? CounterResetDate { get; set; }

    #region Navigation properties

    /// <summary>
    /// The navigation property for the related requests.
    /// </summary>
    [JsonIgnore]
    public virtual IList<Request>? Requests { get; set; }

    #endregion
}