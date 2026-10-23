using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.SystemErrors.Domain;

internal class SystemError : BaseEntity
{
    public Guid ErrorId { get; set; }
    public int Code { get; set; }
    public int Count { get; set; }
    public ErrorSource Source { get; set; }
    public ErrorClassification Classification { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public bool IsMuted { get; set; }
    public List<string> Parameters { get; set; } = [];
    public List<ResponseAction> PossibleResponses { get; set; } = [];

    [MaxLength(Constants.InstrumentSerialNumberLength)]
    public string? SerialNumber { get; set; }

    public ResponseAction? Response { get; set; }
    public DateTimeOffset? ResponseTimestamp { get; set; }
}

public enum ErrorSource
{
    Instrument = 1,
    ProcessCore = 2,
    Pharos = 3
}

public enum ResponseAction
{
    // User response, NOT sent to instrument.
    Acknowledge = 0,

    // Auto response, NOT sent to instrument.
    AutoResolve = 1,

    // User responses, sent to instrument.
    Abort = 2,
    Ignore = 3,
    Retry = 4,
    Skip = 5,
    SkipAll = 6,
    Stop = 7
}