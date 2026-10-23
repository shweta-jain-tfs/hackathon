using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.HL7.Application.Messages;

namespace Pharos.Server.Lis.HL7.Application.Mappings;

internal static class HL7SpecimenSegmentExtensions
{
    public static void MapFromSample(this HL7SpecimenSegment segment, LisSample sample, HL7ExportSettings exportSettings)
    {
        segment.SequenceNumber = 1;
        segment.SpecimenType = HL7SpecimenType.Serum;
        segment.ContainerType = exportSettings.UseLegacyValueForGenderAndTubeType && sample.TubeType == "C"
            ? "N"
            : sample.TubeType;
    }
}
