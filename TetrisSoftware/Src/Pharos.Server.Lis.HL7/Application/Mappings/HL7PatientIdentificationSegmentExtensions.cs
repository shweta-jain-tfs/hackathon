using Pharos.Server.Application.Lis;
using Pharos.Server.Domain;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.HL7.Application.Messages;

namespace Pharos.Server.Lis.HL7.Application.Mappings;

internal static class HL7PatientIdentificationSegmentExtensions
{
    public static void MapFromPatient(this HL7PatientIdentificationSegment segment, LisPatient patient, HL7ExportSettings exportSettings)
    {
        var nameParts = SplitValue(patient.Name, ',');
        var addressParts = SplitLines(patient.Address);

        segment.PatientId = patient.PracticePatientId ?? "";
        segment.PatientListId = patient.MedicalRecordNumber ?? patient.PracticePatientId ?? "";
        segment.AlternatePatientId = patient.PatientNumber ?? "";
        segment.LastName = ValueAt(nameParts, 0);
        segment.FirstName = ValueAt(nameParts, 1);
        segment.MiddleName = ValueAt(nameParts, 2);
        segment.BirthDate = patient.BirthDate;
        segment.Gender = patient.Gender;
        segment.UseLegacyValueForGender = exportSettings.UseLegacyValueForGenderAndTubeType;
        segment.StreetAddress = ValueAt(addressParts, 0);
        segment.OtherAddress = ValueAt(addressParts, 1);
        segment.City = ValueAt(addressParts, 2);
        segment.State = ValueAt(addressParts, 3);
        segment.ZipCode = patient.ZipCode ?? "";
        segment.Country = ValueAt(addressParts, 4);
    }

    public static LisPatient MapToPatient(this HL7PatientIdentificationSegment segment, bool calculatePatientAge)
    {
        return new LisPatient
        {
            PracticePatientId = GetValidPracticePatientId(segment),
            PatientNumber = segment.AlternatePatientId,
            Name = CombineValues(", ", segment.LastName, segment.FirstName, segment.MiddleName),
            BirthDate = segment.BirthDate,
            Gender = segment.Gender,
            Address = CombineValues(Environment.NewLine, segment.StreetAddress, segment.City, segment.State, segment.ZipCode, segment.Country),
            ZipCode = segment.ZipCode,
            CalculateAge = calculatePatientAge
        };
    }

    private static string? GetValidPracticePatientId(HL7PatientIdentificationSegment segment)
    {
        var patientId = !string.IsNullOrEmpty(segment.PatientListId) ? segment.PatientListId : segment.PatientId;

        return patientId.Length <= Constants.PatientIdLength
            ? patientId
            : null;
    }

    private static string CombineValues(string separator, params string[] values)
    {
        return string.Join(separator, values.Where(v => !string.IsNullOrEmpty(v)));
    }

    private static string ValueAt(string[] values, int index)
    {
        return index < values.Length ? values[index] : "";
    }

    private static string[] SplitValue(string? value, char separator)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(separator, StringSplitOptions.TrimEntries);
    }

    private static string[] SplitLines(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Replace("\r", "").Split('\n', StringSplitOptions.TrimEntries);
    }
}
