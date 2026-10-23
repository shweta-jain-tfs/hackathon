using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.HL7.Application.Messages;

namespace Pharos.Server.Lis.HL7.Application.Mappings;

internal static class HL7SpecimenContainerSegmentFactory
{
    public static HL7SpecimenContainerSegment CreateFromSample(LisSample sample, HL7ExportSettings exportSettings)
    {
        var traySource = sample.TestOrders.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.TrayId) || x.TubePosition.HasValue);
        var sampleId = sample.IsSampleLocallyCreated && !exportSettings.UseLocalSampleIdAsLisSampleId
            ? string.Empty
            : sample.SampleId;

        return new HL7SpecimenContainerSegment(
            sampleId,
            traySource?.TrayId ?? "",
            traySource?.TubePosition ?? 0);
    }
}
