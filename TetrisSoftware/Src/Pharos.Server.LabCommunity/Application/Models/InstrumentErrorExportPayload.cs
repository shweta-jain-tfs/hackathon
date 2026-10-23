using Pharos.Server.Domain.Enums;

namespace Pharos.Server.LabCommunity.Application.Models;

internal sealed class InstrumentErrorExportPayload
{
    public InstrumentType InstrumentType { get; init; }

    public string SerialNumber { get; init; } = string.Empty;

    public string ErrorCode { get; init; } = string.Empty;

    public string ErrorText { get; init; } = string.Empty;

    public DateTimeOffset ErrorTime { get; init; }

    public ErrorClassification Classification { get; init; }
}
