using Pharos.Server.Application.Lis;
using Pharos.Server.Lis.HL7.Application.Messages;

namespace Pharos.Server.Lis.HL7.Application.Mappings;

internal static class HL7AdditionalTestInformationSegmentExtensions
{
    public static void MapToSymptoms(this HL7AdditionalTestInformationSegment segment, LisPatient patient)
    {
        if (segment.Symptoms is { Count: > 0 })
        {
            foreach (var symptom in segment.Symptoms)
            {
                patient.Symptoms.Add(new LisSymptom
                {
                    SymptomType = symptom
                });
            }
        }
    }
}
