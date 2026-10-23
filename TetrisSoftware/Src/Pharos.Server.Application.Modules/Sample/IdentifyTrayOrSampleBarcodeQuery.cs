using MediatR;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Sample;

public record IdentifyTrayOrSampleBarcodeQuery(string Barcode) : IRequest<SampleBarcodeResult>;

public class SampleBarcodeResult
{
    public BarcodeType Type { get; init; }
    public int RequestId { get; init; }
    public bool IsTrayInClosedDrawer { get; init; }
    public bool CanQueryMissingRequest { get; init; }

    public SampleBarcodeResult(BarcodeType type, int requestId = 0, bool isTrayInClosedDrawer = false, bool canQueryMissingRequest = false)
    {
        Type = type;
        RequestId = requestId;
        IsTrayInClosedDrawer = isTrayInClosedDrawer;
        CanQueryMissingRequest = canQueryMissingRequest;
    }
}